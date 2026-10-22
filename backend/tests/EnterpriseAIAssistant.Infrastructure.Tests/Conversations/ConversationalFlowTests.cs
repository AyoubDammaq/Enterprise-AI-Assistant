using EnterpriseAIAssistant.Infrastructure.AI;
using EnterpriseAIAssistant.Infrastructure.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Conversations
{
    /// <summary>
    /// Vérifie le comportement conversationnel de bout en bout du
    /// SemanticKernelChatService : ce qui est réellement envoyé au LLM
    /// (IChatCompletionService) pour chaque conversation.
    /// </summary>
    public class ConversationalFlowTests
    {
        private static SemanticKernelChatService CreateService(
            RecordingChatCompletionService recording,
            InMemoryConversationStore store)
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddSingleton<IChatCompletionService>(recording);
            var kernel = kernelBuilder.Build();

            return new SemanticKernelChatService(
                kernel,
                store,
                new InMemoryMemoryService(),
                new MemoryUserContext(),
                NullLogger<SemanticKernelChatService>.Instance);
        }

        [Fact]
        public async Task ConversationA_SecondCall_ContainsItsOwnHistory()
        {
            var recording = new RecordingChatCompletionService();
            var service = CreateService(recording, new InMemoryConversationStore());
            var userId = Guid.NewGuid();

            // Conversation A
            await service.GenerateResponseAsync("My name is Ahmed.", "A", userId);
            await service.GenerateResponseAsync("What is my name?", "A", userId);

            // Le 2e appel au LLM pour A doit contenir tout l'historique de A.
            var secondCallToA = recording.Invocations[1];

            Assert.Contains(secondCallToA, m => m.Content == "My name is Ahmed.");
            Assert.Contains(secondCallToA, m => m.Content == "What is my name?");
        }

        [Fact]
        public async Task ConversationB_DoesNotContainAnyMessageFromA()
        {
            var recording = new RecordingChatCompletionService();
            var store = new InMemoryConversationStore();
            var service = CreateService(recording, store);
            var userId = Guid.NewGuid();

            // Conversation A
            await service.GenerateResponseAsync("My name is Ahmed.", "A", userId);
            await service.GenerateResponseAsync("What is my name?", "A", userId);

            // Conversation B (indépendante)
            await service.GenerateResponseAsync("What is my name?", "B", userId);

            Assert.Equal(3, recording.Invocations.Count);

            var callToB = recording.Invocations[2];

            // Aucun message de A ne doit fuiter dans B.
            Assert.DoesNotContain(callToB, m => m.Content == "My name is Ahmed.");

            // B ne possède qu'un seul message utilisateur : le sien.
            Assert.Single(callToB, m => m.Role == AuthorRole.User);
            Assert.Contains(callToB, m =>
                m.Role == AuthorRole.User && m.Content == "What is my name?");
        }

        /// <summary>
        /// Faux IChatCompletionService : il ne contacte aucun modèle, mais
        /// enregistre une copie (snapshot) de l'historique reçu à chaque appel,
        /// puis renvoie une réponse neutre.
        /// </summary>
        private sealed class RecordingChatCompletionService : IChatCompletionService
        {
            public List<IReadOnlyList<ChatMessageContent>> Invocations { get; } = new();

            public IReadOnlyDictionary<string, object?> Attributes { get; } =
                new Dictionary<string, object?>();

            public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default)
            {
                // Snapshot immuable : l'historique est muté après l'appel.
                var snapshot = chatHistory
                    .Select(m => new ChatMessageContent(m.Role, m.Content))
                    .ToList();

                Invocations.Add(snapshot);

                IReadOnlyList<ChatMessageContent> result =
                [
                    new ChatMessageContent(AuthorRole.Assistant, "ok")
                ];

                return Task.FromResult(result);
            }

            public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
                ChatHistory chatHistory,
                PromptExecutionSettings? executionSettings = null,
                Kernel? kernel = null,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }
    }
}
