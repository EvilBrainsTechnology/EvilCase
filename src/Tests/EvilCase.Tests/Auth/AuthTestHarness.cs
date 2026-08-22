using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// An <see cref="IAuthService"/> wired to in-memory stores and a clock a test can move. Nothing here
/// opens a connection.
/// </summary>
internal sealed class AuthTestHarness
{
    public const string Email = "user@evilcase.test";

    public const string Password = "correct-horse-battery-staple";

    public const int MaxFailedAttempts = 5;

    public static readonly Guid Tenant = Guid.CreateVersion7();

    /// <summary>
    /// Hashed once: PBKDF2 is deliberately slow and every test would otherwise pay for it again.
    /// </summary>
    private static readonly string PasswordHash = PasswordHasher.Hash(Password);

    private static readonly DateTime Start = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    public AuthTestHarness()
    {
        this.Time = new(Start);
        this.Settings = CreateSettings();

        this.User = this.Users.Seed(
            new()
            {
                TenantId = Tenant,
                Email = Email,
                PasswordHash = PasswordHash,
                Role = UserRole.Admin,
                DefaultContactId = Guid.CreateVersion7(),
            });

        this.Service = new AuthService(
            this.Users,
            this.RefreshTokens,
            new AuthTokenService(Options.Create(this.Settings), this.Time),
            Options.Create(this.Settings),
            this.Time,
            NullLogger<AuthService>.Instance);
    }

    public FakeUserStore Users { get; } = new();

    public FakeRefreshTokenStore RefreshTokens { get; } = new();

    public TestTimeProvider Time { get; }

    public AuthSettings Settings { get; }

    public User User { get; }

    public IAuthService Service { get; }

    public static AuthSettings CreateSettings()
    {
        return new()
        {
            Jwt = new()
            {
                Issuer = "https://auth.evilcase.test",
                Audience = "EvilCase",
                AccessTokenExpiration = TimeSpan.FromMinutes(15),
                Key = new string('k', 64),
            },
            RefreshToken = new()
            {
                Expiration = TimeSpan.FromDays(14),
                SessionExpiration = TimeSpan.FromDays(30),
            },
            Lockout = new()
            {
                MaxFailedAttempts = MaxFailedAttempts,
                Duration = TimeSpan.FromMinutes(15),
            },
        };
    }

    public async Task<AuthSession> SignIn()
    {
        var result = await this.Service.Login(Email, Password, ClientInfo.Unknown, CancellationToken.None);

        return result.Session ?? throw new InvalidOperationException("The harness could not sign in");
    }

    public Task<LoginResult> Login(string password)
    {
        return this.Service.Login(Email, password, ClientInfo.Unknown, CancellationToken.None);
    }

    public async Task<AuthSession?> Refresh(string refreshToken)
    {
        return (await this.RefreshOutcome(refreshToken)).Session;
    }

    public Task<RefreshResult> RefreshOutcome(string refreshToken)
    {
        return this.Service.Refresh(refreshToken, ClientInfo.Unknown, CancellationToken.None);
    }
}
