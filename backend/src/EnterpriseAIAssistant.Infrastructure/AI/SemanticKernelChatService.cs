using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAIAssistant.Infrastructure.AI
{
    public class SemanticKernelChatService(Kernel kernel) : IAIChatService
    {
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

            var response =
                await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    cancellationToken: cancellationToken);

            return response.Content ?? string.Empty;
        }
    }
}
