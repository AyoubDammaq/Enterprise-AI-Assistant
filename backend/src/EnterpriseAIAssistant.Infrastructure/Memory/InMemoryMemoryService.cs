using EnterpriseAIAssistant.Application.Interfaces;
using System.Collections.Concurrent;

namespace EnterpriseAIAssistant.Infrastructure.Memory
{
    public sealed class InMemoryMemoryService : IMemoryService
    {
        private readonly ConcurrentDictionary<
            string,
            string> _memory = new();

        public Task SaveAsync(
            string userId,
            string key,
            string value,
            CancellationToken cancellationToken = default)
        {
            Validate(userId, key);
            ArgumentNullException.ThrowIfNull(value);
            cancellationToken.ThrowIfCancellationRequested();

            var storageKey = CreateStorageKey(userId, key);

            _memory[storageKey] = value;

            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(
            string userId,
            string key,
            CancellationToken cancellationToken = default)
        {
            Validate(userId, key);
            cancellationToken.ThrowIfCancellationRequested();

            var storageKey = CreateStorageKey(userId, key);

            _memory.TryGetValue(storageKey, out var value);

            return Task.FromResult(value);
        }

        public Task DeleteAsync(
            string userId,
            string key,
            CancellationToken cancellationToken = default)
        {
            Validate(userId, key);
            cancellationToken.ThrowIfCancellationRequested();

            var storageKey = CreateStorageKey(userId, key);

            _memory.TryRemove(storageKey, out _);

            return Task.CompletedTask;
        }

        private static string CreateStorageKey(
            string userId,
            string key)
        {
            return $"{userId.Trim()}:{key.Trim()}";
        }

        private static void Validate(string userId, string key)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException(
                    "User ID cannot be empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(
                    "Memory key cannot be empty.", nameof(key));
        }
    }
}
