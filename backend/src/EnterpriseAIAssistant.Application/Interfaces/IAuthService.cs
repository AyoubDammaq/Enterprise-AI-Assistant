using EnterpriseAIAssistant.Application.Auth;

namespace EnterpriseAIAssistant.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken);

        Task<AuthResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken);

        Task<AuthResponse> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken);

        Task RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken);
    }
}
