using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IConversationStore
    {
        /// <summary>Retourne l'historique existant ou en crée un nouveau.</summary>
        ChatHistory GetOrCreate(string conversationId, Guid ownerId);

        /// <summary>
        /// Tente de récupérer un historique existant sans le créer.
        /// Retourne false si la conversation est inconnue.
        /// </summary>
        bool TryGet(string conversationId, Guid ownerId, out ChatHistory? history);

        /// <summary>
        /// Supprime une conversation. Retourne true si elle existait.
        /// </summary>
        bool Delete(string conversationId, Guid ownerId);
    }
}
