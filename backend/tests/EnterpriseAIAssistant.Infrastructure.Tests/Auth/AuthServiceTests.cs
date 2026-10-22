using System.Security.Cryptography;
using EnterpriseAIAssistant.Application.Auth;
using EnterpriseAIAssistant.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldReject_WhenPasswordIsIncorrect()
    {
        using var h = CreateHarness();
        var user = NewUser();

        h.Users.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        h.Users.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        h.Users.Setup(x => x.CheckPasswordAsync(user, "WrongPassword!123"))
            .ReturnsAsync(false);

        h.Users.Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var request = new LoginRequest
            (user.Email!, 
            "WrongPassword!123");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => h.Sut.LoginAsync(request, CancellationToken.None));

        h.Users.Verify(x => x.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReject_WhenUserIsLockedOut()
    {
        using var h = CreateHarness();
        var user = NewUser();

        h.Users.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        h.Users.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        var request = new LoginRequest(user.Email!, "CorrectPassword!123");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => h.Sut.LoginAsync(request, CancellationToken.None));

        h.Users.Verify(
            x => x.CheckPasswordAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        using var h = CreateHarness();
        var user = NewUser();

        h.Users.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        h.Users.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        h.Users.Setup(x => x.CheckPasswordAsync(user, "CorrectPassword!123"))
            .ReturnsAsync(true);

        h.Users.Setup(x => x.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        h.Users.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync((IList<string>)["User"]);

        var request = new LoginRequest(user.Email!, "CorrectPassword!123");

        var result = await h.Sut.LoginAsync(
            request, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.True(result.AccessTokenExpiresAtUtc > DateTime.UtcNow);
        Assert.True(result.RefreshTokenExpiresAtUtc > DateTime.UtcNow);

        Assert.Equal(1, await h.Db.RefreshTokens.CountAsync());
        h.Users.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldReject_WhenTokenDoesNotExist()
    {
        using var h = CreateHarness();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => h.Sut.RefreshAsync(
                "unknown-refresh-token",
                CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_ShouldReject_WhenTokenIsRevoked()
    {
        using var h = CreateHarness();
        var user = NewUser();
        var rawToken = "revoked-refresh-token";

        h.Db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            TokenHash = JwtTokenService.HashRefreshToken(rawToken),
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });

        await h.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => h.Sut.RefreshAsync(rawToken, CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAsync_ShouldNotThrow_WhenTokenDoesNotExist()
    {
        using var h = CreateHarness();

        await h.Sut.RevokeAsync(
            "unknown-refresh-token",
            CancellationToken.None);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReject_WhenEmailAlreadyExists()
    {
        using var h = CreateHarness();
        var user = NewUser();

        h.Users.Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        var request = new RegisterRequest(user.Email!, "CorrectPassword!123");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => h.Sut.RegisterAsync(request, CancellationToken.None));

        h.Users.Verify(
            x => x.CreateAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static ApplicationUser NewUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = "auth-test@example.com",
        Email = "auth-test@example.com"
    };

    private static Harness CreateHarness()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AuthDbContext(options);

        var identityOptions = Options.Create(new IdentityOptions());

        var userStore = new Mock<IUserStore<ApplicationUser>>();

        var users = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            identityOptions,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();

        var roles = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object,
            Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole<Guid>>>>());

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "EnterpriseAIAssistant.Tests",
            Audience = "EnterpriseAIAssistant.Tests.Client",
            SecretKey = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64)),
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        });

        var jwtService = new JwtTokenService(jwtOptions);

        var sut = new AuthService(
            users.Object,
            roles.Object,
            db,
            jwtService,
            jwtOptions);

        return new Harness(db, users, sut);
    }

    private sealed class Harness(
        AuthDbContext db,
        Mock<UserManager<ApplicationUser>> users,
        AuthService sut) : IDisposable
    {
        public AuthDbContext Db { get; } = db;
        public Mock<UserManager<ApplicationUser>> Users { get; } = users;
        public AuthService Sut { get; } = sut;

        public void Dispose() => Db.Dispose();
    }
}
