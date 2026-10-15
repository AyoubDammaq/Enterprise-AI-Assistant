using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IConversationStore
    {
        ChatHistory GetOrCreate(string conversationId);
    }
}
