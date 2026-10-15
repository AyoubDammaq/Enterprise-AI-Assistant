using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace EnterpriseAIAssistant.Infrastructure.Memory
{
    public sealed class InMemoryConversationStore : IConversationStore
    {
        private readonly ConcurrentDictionary<string, ChatHistory> _conversations
            = new();

        public ChatHistory GetOrCreate(string conversationId)
        {
            return _conversations.GetOrAdd(
                conversationId,
                _ => new ChatHistory()
            );
        }
    }
}
