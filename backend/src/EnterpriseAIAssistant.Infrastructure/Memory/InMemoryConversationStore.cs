using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.Concurrent;

namespace EnterpriseAIAssistant.Infrastructure.Memory
{
    public sealed class InMemoryConversationStore : IConversationStore
    {
        private sealed record ConversationEntry(
            Guid OwnerId,
            ChatHistory History);

        private readonly ConcurrentDictionary<string, ConversationEntry>
            _conversations = new();

        public ChatHistory GetOrCreate(
            string conversationId,
            Guid ownerId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentException(
                    "Conversation ID is required.",
                    nameof(conversationId));

            if (ownerId == Guid.Empty)
                throw new ArgumentException(
                    "A valid owner ID is required.",
                    nameof(ownerId));

            var entry = _conversations.GetOrAdd(
                conversationId,
                _ => new ConversationEntry(
                    ownerId,
                    new ChatHistory()));

            if (entry.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException(
                    "Access to this conversation is denied.");
            }

            return entry.History;
        }

        public bool TryGet(
            string conversationId,
            Guid ownerId,
            out ChatHistory? history)
        {
            history = null;

            if (string.IsNullOrWhiteSpace(conversationId) ||
                ownerId == Guid.Empty)
            {
                return false;
            }

            if (!_conversations.TryGetValue(
                    conversationId,
                    out var entry))
            {
                return false;
            }

            if (entry.OwnerId != ownerId)
                return false;

            history = entry.History;
            return true;
        }

        public bool Delete(string conversationId, Guid ownerId)
        {
            if (string.IsNullOrWhiteSpace(conversationId) ||
                ownerId == Guid.Empty)
            {
                return false;
            }

            // Vérifie le propriétaire et supprime l'entrée correspondante
            // de façon atomique, sans supprimer celle d'un autre utilisateur.
            if (!_conversations.TryGetValue(
                    conversationId,
                    out var entry) ||
                entry.OwnerId != ownerId)
            {
                return false;
            }

            return ((ICollection<KeyValuePair<string, ConversationEntry>>)
                _conversations).Remove(
                    new KeyValuePair<string, ConversationEntry>(
                        conversationId,
                        entry));
        }
    }
}