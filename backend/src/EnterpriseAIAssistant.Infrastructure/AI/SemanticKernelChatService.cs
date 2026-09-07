using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace EnterpriseAIAssistant.Infrastructure.AI
{
    public class SemanticKernelChatService(Kernel kernel) : IAIChatService
    {
        private readonly Kernel _kernel = kernel;
        private readonly IChatCompletionService _chatCompletionService =
                kernel.GetRequiredService<IChatCompletionService>();

        public async Task<string> GenerateResponseAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine();
            Console.WriteLine("======================================");
            Console.WriteLine($">>> REQUEST: {prompt}");
            Console.WriteLine("======================================");

            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(
                """
                You are an AI assistant integrated into an enterprise application.

                Your responsibilities:
                - Provide clear and accurate answers.
                - Be concise but useful.
                - Explain technical concepts in a simple way when necessary.
                - Do not invent information.

                Tool selection rules:

                CalculatorPlugin:
                - Use ONLY for arithmetic calculations.
                - Examples:
                  - "What is 25 * 12?"
                  - "Calculate 100 / 4"
                  - "What is 20 + 15?"
                - Never use CalculatorPlugin for definitions, explanations, or general questions.

                DateTimePlugin:
                - Use ONLY when the user explicitly asks for the current date or current time.
                - Examples:
                  - "What time is it?"
                  - "What is the current date?"
                  - "What date is it today?"
                - Do NOT use DateTimePlugin for questions such as:
                  - "What is dependency injection?"
                  - "What is ASP.NET Core?"
                  - "Explain Semantic Kernel"
                  - "What is C#?"
                  - "What is .NET?"

                No-tool questions:
                - For general knowledge, definitions, explanations, programming concepts,
                  and technical questions, answer directly.
                - Do not call any tool.
                - Do not invent a tool.
                - Do not output JSON representing a tool call.
                """);

            chatHistory.AddUserMessage(prompt);

            var executionSettings = new OllamaPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0
            };

            var response =
            await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);

            Console.WriteLine("========== FINAL MODEL RESPONSE ==========");
            Console.WriteLine($"Content: {response.Content}");

            Console.WriteLine("========== RESPONSE ITEMS ==========");

            foreach (var item in response.Items)
            {
                Console.WriteLine($"TYPE: {item.GetType().FullName}");
                Console.WriteLine($"ITEM: {item}");
            }

            Console.WriteLine("======================================");
            Console.WriteLine($">>> END REQUEST: {prompt}");
            Console.WriteLine("======================================");

            return response.Content ?? string.Empty;
        }
    }
}