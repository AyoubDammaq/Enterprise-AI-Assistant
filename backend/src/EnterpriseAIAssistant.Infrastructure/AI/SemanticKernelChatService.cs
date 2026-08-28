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

            // Boucle manuelle du cycle Tool Call pour compatibilité avec Ollama
            while (true)
            {
                var response = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: executionSettings,
                    kernel: _kernel,
                    cancellationToken: cancellationToken);

                Console.WriteLine("========== MODEL RESPONSE ==========");
                Console.WriteLine($"Content: {response.Content}");

                Console.WriteLine("========== RESPONSE ITEMS ==========");

                foreach (var item in response.Items)
                {
                    Console.WriteLine($"Item type: {item.GetType().Name}");
                    Console.WriteLine($"Item: {item}");
                }

                // Extraire les appels de fonctions dans la réponse
                var functionCalls = FunctionCallContent.GetFunctionCalls(response).ToList();

                Console.WriteLine($"========== FUNCTION CALLS: {functionCalls.Count} ==========");

                foreach (var functionCall in functionCalls)
                {
                    Console.WriteLine(
                        $"Plugin={functionCall.PluginName}, " +
                        $"Function={functionCall.FunctionName}");
                }

                // Aucun tool call → réponse finale
                if (functionCalls.Count == 0)
                {
                    return response.Content ?? string.Empty;
                }

                // Ajouter la réponse de l'assistant (avec les tool calls) à l'historique
                chatHistory.Add(response);

                // Invoquer chaque fonction et ajouter les résultats à l'historique
                foreach (var functionCall in functionCalls)
                {
                    var resultContent = await functionCall.InvokeAsync(_kernel, cancellationToken);
                    chatHistory.Add(resultContent.ToChatMessage());
                }
                // Continuer la boucle → le LLM reçoit les résultats et génère la réponse finale
            }
        }
    }
}