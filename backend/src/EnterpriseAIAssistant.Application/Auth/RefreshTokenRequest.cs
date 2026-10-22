using System.ComponentModel.DataAnnotations;

namespace EnterpriseAIAssistant.Application.Auth
{
    public sealed record RefreshTokenRequest(
    [Required] string RefreshToken);
}
