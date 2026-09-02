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
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(
                """
            You are an AI assistant integrated into an enterprise application.

            Your responsibilities:
            - Provide clear and accurate answers.
            - Be concise but useful.
            - Explain technical concepts in a simple way when necessary.
            - Do not invent information.
            - If you do not know something, clearly say so.
            """);

            chatHistory.AddUserMessage(prompt);

            var executionSettings = new OllamaPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var response =
            await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);

            Console.WriteLine("========== MODEL RESPONSE ==========");
            Console.WriteLine($"Content: {response.Content}");

            Console.WriteLine("========== RESPONSE ITEMS ==========");

            foreach (var item in response.Items)
            {
                Console.WriteLine($"TYPE: {item.GetType().FullName}");
                Console.WriteLine($"ITEM: {item}");
            }

            return response.Content ?? string.Empty;
        }
    }
}