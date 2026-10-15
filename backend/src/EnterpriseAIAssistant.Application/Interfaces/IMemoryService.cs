namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IMemoryService
    {
        Task SaveAsync(
            string userId,
            string key,
            string value,
            CancellationToken cancellationToken = default);

        Task<string?> GetAsync(
            string userId,
            string key,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string userId,
            string key,
            CancellationToken cancellationToken = default);
    }
}
