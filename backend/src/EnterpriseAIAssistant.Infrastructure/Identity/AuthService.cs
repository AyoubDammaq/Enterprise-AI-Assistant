using EnterpriseAIAssistant.Application.Auth;
using EnterpriseAIAssistant.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseAIAssistant.Infrastructure.Identity
{
    public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    AuthDbContext dbContext,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
    {
        private readonly JwtOptions _jwtOptions = jwtOptions.Value;
        public async Task<AuthResponse> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await userManager.FindByEmailAsync(email) is not null)
            {
                throw new InvalidOperationException(
                    "Un compte avec cette adresse email existe déjà.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email
            };

            var createResult = await userManager.CreateAsync(
                user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(errors);
            }

            const string defaultRole = "User";

            if (!await roleManager.RoleExistsAsync(defaultRole))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole<Guid>(defaultRole));

                if (!roleResult.Succeeded)
                {
                    await userManager.DeleteAsync(user);

                    throw new InvalidOperationException(
                        "Impossible de créer le rôle utilisateur.");
                }
            }

            var addRoleResult = await userManager.AddToRoleAsync(
                user, defaultRole);

            if (!addRoleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);

                throw new InvalidOperationException(
                    "Impossible d'attribuer le rôle utilisateur.");
            }

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await userManager.FindByEmailAsync(email);

            // Réponse générique pour un compte inexistant
            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "Email ou mot de passe incorrect.");
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException(
                    "Email ou mot de passe incorrect.");
            }

            var passwordValid = await userManager.CheckPasswordAsync(
                user, request.Password);

            if (!passwordValid)
            {
                await userManager.AccessFailedAsync(user);

                throw new UnauthorizedAccessException(
                    "Email ou mot de passe incorrect.");
            }

            await userManager.ResetAccessFailedCountAsync(user);

            return await IssueTokensAsync(user, cancellationToken);
        }

        public async Task<AuthResponse> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            var hash = JwtTokenService.HashRefreshToken(refreshToken);
            var now = DateTime.UtcNow;

            var storedToken = await dbContext.RefreshTokens
                .Include(x => x.User)
                .SingleOrDefaultAsync(
                    x => x.TokenHash == hash,
                    cancellationToken);

            if (storedToken is null ||
                storedToken.RevokedAtUtc is not null ||
                storedToken.ExpiresAtUtc <= now)
            {
                throw new UnauthorizedAccessException(
                    "Refresh token invalide, expiré ou révoqué.");
            }

            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            storedToken.RevokedAtUtc = now;

            var replacement = JwtTokenService.CreateRefreshToken();
            var replacementHash =
                JwtTokenService.HashRefreshToken(replacement);

            storedToken.ReplacedByTokenHash = replacementHash;

            var roles = await userManager.GetRolesAsync(storedToken.User);
            var (accessToken, accessExpiry) =
                jwtTokenService.CreateAccessToken(storedToken.User, roles);

            var refreshExpiry = now.AddDays(_jwtOptions.RefreshTokenDays);

            dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = storedToken.UserId,
                TokenHash = replacementHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = refreshExpiry
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AuthResponse(
                accessToken,
                accessExpiry,
                replacement,
                refreshExpiry);
        }

        public async Task RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            var hash = JwtTokenService.HashRefreshToken(refreshToken);

            var storedToken = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(
                    x => x.TokenHash == hash,
                    cancellationToken);

            if (storedToken is null || storedToken.RevokedAtUtc is not null)
            {
                return;
            }

            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<AuthResponse> IssueTokensAsync(
    ApplicationUser user,
    CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var roles = await userManager.GetRolesAsync(user);

            var (accessToken, accessExpiry) =
                jwtTokenService.CreateAccessToken(user, roles);

            var refreshToken = JwtTokenService.CreateRefreshToken();
            var refreshExpiry = now.AddDays(_jwtOptions.RefreshTokenDays);

            dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = JwtTokenService.HashRefreshToken(refreshToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = refreshExpiry
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                accessToken,
                accessExpiry,
                refreshToken,
                refreshExpiry);
        }
    }
}
