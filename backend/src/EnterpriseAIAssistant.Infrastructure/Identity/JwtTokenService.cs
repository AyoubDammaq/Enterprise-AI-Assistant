using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAIAssistant.Infrastructure.Identity
{
    public sealed class JwtTokenService(IOptions<JwtOptions> options)
    {
        private readonly JwtOptions _options = options.Value;

        public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
            ApplicationUser user,
            IList<string> roles)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.AccessTokenMinutes);

            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException(
                    "Jwt:SecretKey est absent des options JWT injectées.");
            }

            if (string.IsNullOrWhiteSpace(_options.Issuer) ||
                string.IsNullOrWhiteSpace(_options.Audience))
            {
                throw new InvalidOperationException(
                    "La configuration Issuer/Audience JWT est incomplète.");
            }

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.AddRange(roles.Select(role => new Claim("role", role)));

            var key = new SymmetricSecurityKey(
                Convert.FromBase64String(_options.SecretKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
        }

        public static string CreateRefreshToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64));
        }

        public static string HashRefreshToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }
    }
}
