using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.SemanticKernel;

namespace EnterpriseAIAssistant.Infrastructure.AI.Ollama
{
    public class OllamaChatService(Kernel kernel) : IAIChatService
    {
        private readonly Kernel _kernel = kernel;

        public async Task<string> GenerateResponseAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var result = await _kernel.InvokePromptAsync(
                prompt,
                cancellationToken: cancellationToken);

            return result.ToString();
        }
    }
}
