
using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;

namespace EnterpriseAIAssistant.Application.Services
{
    public sealed class RagService(
        IKnowledgeRetriever knowledgeRetriever,
        IContextRelevanceEvaluator contextRelevanceEvaluator,
        IAIChatService aiChatService) : IRagService
    {
        private const string NoContextMessage =
            "I couldn't find relevant information in the available documents.";

        public async Task<RagResponse> AskAsync(
            string question,
            string conversationId,
            Guid? userId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(question);

            var retrievedChunks = await knowledgeRetriever.SearchAsync(
                question,
                cancellationToken: cancellationToken);

            if (retrievedChunks.Count == 0)
            {
                return new RagResponse(
                    NoContextMessage,
                    Array.Empty<RagSource>());
            }

            var isContextSufficient =
                await contextRelevanceEvaluator.IsContextSufficientAsync(
                    question,
                    retrievedChunks,
                    cancellationToken);

            if (!isContextSufficient)
            {
                return new RagResponse(
                    NoContextMessage,
                    Array.Empty<RagSource>());
            }

            var answer = await aiChatService.GenerateGroundedResponseAsync(
                question,
                retrievedChunks,
                conversationId,
                userId,
                cancellationToken);

            var sources = retrievedChunks
                .Select(result => new RagSource(
                    result.Chunk.DocumentId,
                    result.Chunk.Source,
                    result.Chunk.ChunkIndex))
                .DistinctBy(source => new
                {
                    source.DocumentId,
                    source.ChunkIndex
                })
                .ToArray();

            return new RagResponse(answer, sources);
        }
    }
}
