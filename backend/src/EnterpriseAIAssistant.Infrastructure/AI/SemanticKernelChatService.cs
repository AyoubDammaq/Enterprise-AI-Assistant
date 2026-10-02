using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Diagnostics;

namespace EnterpriseAIAssistant.Infrastructure.AI
{
    public class SemanticKernelChatService(
        Kernel kernel,
        ILogger<SemanticKernelChatService> logger) : IAIChatService
    {
        private readonly Kernel _kernel = kernel;
        private readonly ILogger<SemanticKernelChatService> _logger = logger;
        private readonly IChatCompletionService _chatCompletionService =
                kernel.GetRequiredService<IChatCompletionService>();

        public async Task<string> GenerateResponseAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            // Corrèle tous les logs d'une même requête.
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = Guid.NewGuid()
            });

            // --- Request ---
            _logger.LogInformation("[PIPELINE] Request -> {Prompt}", prompt);

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

            _logger.LogDebug(
                "[PIPELINE] Next action -> invoking model with FunctionChoiceBehavior=Auto, Temperature={Temperature}",
                executionSettings.Temperature);

            // NOTE : La sélection de l'outil, ses arguments, son exécution et son
            // résultat sont journalisés par ToolInvocationLoggingFilter
            // (enregistré dans le Kernel via AutoFunctionInvocationFilters).
            var stopwatch = Stopwatch.StartNew();
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);
            stopwatch.Stop();

            // --- Next action / tool usage summary ---
            var functionCalls = response.Items
                .OfType<FunctionCallContent>()
                .ToList();

            if (functionCalls.Count > 0)
            {
                foreach (var call in functionCalls)
                {
                    _logger.LogInformation(
                        "[PIPELINE] Next action -> model requested {Plugin}.{Function}",
                        call.PluginName, call.FunctionName);
                }
            }
            else
            {
                _logger.LogInformation(
                    "[PIPELINE] Next action -> model answered directly (no tool used)");
            }

            // --- Final response ---
            _logger.LogInformation(
                "[PIPELINE] Final response ({ElapsedMs} ms) -> {Content}",
                stopwatch.ElapsedMilliseconds,
                response.Content);

            return response.Content ?? string.Empty;
        }
    }
}