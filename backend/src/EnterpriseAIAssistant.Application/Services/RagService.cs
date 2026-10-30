using EnterpriseAIAssistant.Application.DTOs;
using EnterpriseAIAssistant.Application.Interfaces;

namespace EnterpriseAIAssistant.Application.Services
{
    public sealed class RagService(
        IKnowledgeRetriever knowledgeRetriever,
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

            // 1. Retrieve relevant document chunks.
            var retrievedChunks = await knowledgeRetriever.SearchAsync(
                question,
                cancellationToken: cancellationToken);

            // 2. Do not call the LLM if retrieval returns nothing.
            if (retrievedChunks.Count == 0)
            {
                return new RagResponse(
                    NoContextMessage,
                    Array.Empty<RagSource>());
            }

            // 3. Generate an answer using the original question
            // and the retrieved document context.
            var answer = await aiChatService.GenerateGroundedResponseAsync(
                question,
                retrievedChunks,
                conversationId,
                userId,
                cancellationToken);

            // 4. Map retrieved chunks to source references.
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

            // 5. Return the answer and its sources.
            return new RagResponse(answer, sources);
        }
    }
}
