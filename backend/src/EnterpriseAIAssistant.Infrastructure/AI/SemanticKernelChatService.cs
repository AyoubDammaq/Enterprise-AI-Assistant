using EnterpriseAIAssistant.Application.Interfaces;
using EnterpriseAIAssistant.Infrastructure.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Diagnostics;

namespace EnterpriseAIAssistant.Infrastructure.AI
{
    public class SemanticKernelChatService(
        Kernel kernel,
        IConversationStore conversationStore,
        IMemoryService memoryService,
        MemoryUserContext memoryUserContext,
        ILogger<SemanticKernelChatService> logger) : IAIChatService
    {
        private readonly Kernel _kernel = kernel;
        private readonly ILogger<SemanticKernelChatService> _logger = logger;
        private readonly IConversationStore _conversationStore = conversationStore;
        private readonly IMemoryService _memoryService = memoryService;
        private readonly MemoryUserContext _memoryUserContext = memoryUserContext;
        private readonly IChatCompletionService _chatCompletionService =
                kernel.GetRequiredService<IChatCompletionService>();

        public async Task<string> GenerateResponseAsync(
            string prompt,
            string conversationId,
            Guid? userId,   
            CancellationToken cancellationToken = default)
        {
            // Corrèle tous les logs d'une même requête.
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = Guid.NewGuid()
            });

            // Identifiant utilisateur effectif transmis aux opérations mémoire.
            // On garantit une valeur stable même si l'appelant n'en fournit pas.
            if (userId is null || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A valid userId is required for memory operations.",
                    nameof(userId));
            }

            var effectiveUserId = userId.Value.ToString();

            using var userScope = _memoryUserContext.BeginScope(effectiveUserId);

            // --- Request ---
            _logger.LogInformation(
                "[PIPELINE] Request (UserId={UserId}) -> {Prompt}",
                effectiveUserId,
                prompt);

            var chatHistory = _conversationStore.GetOrCreate(conversationId);


            if (!chatHistory.Any(m => m.Role == AuthorRole.System))
            {
                chatHistory.AddSystemMessage(
                    """
                        You are a helpful AI assistant integrated into an enterprise application.

                        General behavior:
                        - Respond naturally and directly to the user's message.
                        - Be clear, accurate, concise, and helpful.
                        - Do not invent information.
                        - Use previous messages in the current conversation to answer follow-up questions.
                        - Conversation history is not persistent memory.
                        - When the user explicitly asks you to remember a personal fact
                          for future conversations, call MemoryPlugin.SaveMemory.
                        - When a question depends on a previously stored fact, call
                          MemoryPlugin.GetMemory, even if the current conversation is new.
                        - Use the exact userId supplied by the application for every
                          memory operation.
                        - Never claim that a memory was saved unless the tool call succeeded.
                        - If no memory is found, say that the information is not stored.
                        - Never invent personal information or preferences.
                        - You MUST call SaveMemory before claiming anything was saved.
                        - NEVER say a preference was saved unless the SaveMemory tool was actually invoked and returned success.
                        - When the user asks what you remember, you MUST call GetMemory. Do not answer from memory of the chat.

                        Tool selection rules:

                        MemoryPlugin:
                        - Use SaveMemory when the user explicitly asks you to remember
                          a useful fact or preference.
                        - Use GetMemory when a stored fact is relevant to the user's
                          question or when the user asks what you remember.
                        - Use DeleteMemory when the user asks you to forget a stored fact.
                        - Use the same userId supplied by the application for every
                          memory operation. Never invent or change this ID.
                        - Use concise, descriptive keys such as preferred_language.
                        - Never claim that information was saved unless SaveMemory
                          completed successfully.
                        - Do not store passwords, API keys, or other sensitive secrets.

                        CalculatorPlugin:
                        - Use only when the user requests an arithmetic calculation.
                        - Never use for greetings, introductions, or general questions.

                        DateTimePlugin:
                        - Use only when the user explicitly asks for the current date or time.
                        - Never use for unrelated questions.

                        For ordinary conversation:
                        - Answer directly without calling a tool.
                        - Do not fabricate tool calls or claim that a tool was used when it was not.
                        """
                );
            }

            chatHistory.AddUserMessage(prompt);

            var executionSettings = new OllamaPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0
            };

            _logger.LogDebug(
                "[PIPELINE] Next action -> invoking model with FunctionChoiceBehavior=Auto, Temperature={Temperature}",
                executionSettings.Temperature);

            // NOTE : La sélection de l'outil, ses arguments, son exécution et son
            // résultat sont journalisés par ToolInvocationLoggingFilter
            // (enregistré dans le Kernel via AutoFunctionInvocationFilters).
            var stopwatch = Stopwatch.StartNew();
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);
            stopwatch.Stop();

            chatHistory.AddAssistantMessage(response.Content ?? string.Empty);

            // --- Next action / tool usage summary --- 
            var functionCalls = response.Items
                .OfType<FunctionCallContent>()
                .ToList();

            if (functionCalls.Count > 0)
            {
                foreach (var call in functionCalls)
                {
                    _logger.LogInformation(
                        "[PIPELINE] Next action -> model requested {Plugin}.{Function}",
                        call.PluginName, call.FunctionName);
                }
            }
            else
            {
                _logger.LogInformation(
                    "[PIPELINE] Next action -> model answered directly (no tool used)");
            }

            // --- Final response ---
            _logger.LogInformation(
                "[PIPELINE] Final response ({ElapsedMs} ms) -> {Content}",
                stopwatch.ElapsedMilliseconds,
                response.Content);

            return response.Content ?? string.Empty;
        }
    }
}