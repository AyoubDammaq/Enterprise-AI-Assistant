using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EnterpriseAIAssistant.Infrastructure.Memory
{
    public sealed class MemoryPlugin(
        IMemoryService memoryService,
        MemoryUserContext userContext,
        ILogger<MemoryPlugin> logger)
    {
        private readonly IMemoryService _memoryService = memoryService ?? throw new ArgumentNullException(nameof(memoryService));
        private readonly MemoryUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        private readonly ILogger<MemoryPlugin> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [KernelFunction("SaveMemory")]
        [Description("Save a useful fact or preference for the current user so it can be recalled later.")]
        public async Task<string> SaveMemory(
            [Description("A short key identifying the fact, for example preferred_language.")] string key,
            [Description("The value to remember, for example French.")] string value,
            CancellationToken cancellationToken = default)
        {
            var userId = ResolveUserId();
            _logger.LogInformation("[MEMORY PLUGIN] SaveMemory invoked for key {Key}", key);
            ValidateKey(key);
            ValidateValue(value);

            await _memoryService.SaveAsync(userId, key, value, cancellationToken);
            return $"Memory saved successfully for key '{key}'.";
        }

        [KernelFunction("GetMemory")]
        [Description(
            "Retrieve one stored user fact. Use the canonical key 'name' " +
            "for the user's name and 'favorite_football_team' for their " +
            "favorite football club. Query one key per call.")]
        public async Task<string> GetMemory(
            [Description("The key of the memory to retrieve.")] string key,
            CancellationToken cancellationToken = default)
        {
            var userId = ResolveUserId();
            _logger.LogInformation("[MEMORY PLUGIN] GetMemory invoked for key {Key}", key);
            ValidateKey(key);

            var value = await _memoryService.GetAsync(userId, key, cancellationToken);

            _logger.LogInformation(
                "[MEMORY PLUGIN] GetMemory completed. Key={Key}, Found={Found}, UserId={UserId}",
                key, value is not null, userId);

            return value is null
                ? $"No memory found for key '{key}'."
                : value;
        }

        [KernelFunction("DeleteMemory")]
        [Description("Delete a previously saved fact or preference for the current user.")]
        public async Task<string> DeleteMemory(
            [Description("The key of the memory to delete.")] string key,
            CancellationToken cancellationToken = default)
        {
            var userId = ResolveUserId();
            _logger.LogInformation("[MEMORY PLUGIN] DeleteMemory invoked for key {Key}", key);
            ValidateKey(key);

            await _memoryService.DeleteAsync(userId, key, cancellationToken);
            return $"Delete operation completed for key '{key}'.";
        }

        private string ResolveUserId()
        {
            var userId = _userContext.CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException(
                    "No user context was established for this memory operation.");
            return userId;
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Memory key is required.", nameof(key));
        }

        private static void ValidateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Memory value is required.", nameof(value));

            if (value.Length > 2000)
                throw new ArgumentException(
                    "Memory value cannot exceed 2000 characters.", nameof(value));
        }
    }
}
