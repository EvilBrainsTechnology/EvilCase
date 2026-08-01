using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
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
                Email = Email,
                PasswordHash = PasswordHash,
                Role = UserRole.Admin,
                Created = Start,
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

    public static AuthSettings CreateSettings() =>
        new()
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

    public async Task<AuthSession> SignInAsync()
    {
        var result = await this.Service.LoginAsync(Email, Password, ClientInfo.Unknown, CancellationToken.None);

        return result.Session ?? throw new InvalidOperationException("The harness could not sign in");
    }

    public Task<LoginResult> LoginAsync(string password) =>
        this.Service.LoginAsync(Email, password, ClientInfo.Unknown, CancellationToken.None);

    public async Task<AuthSession?> RefreshAsync(string refreshToken) =>
        (await this.RefreshResultAsync(refreshToken)).Session;

    public Task<RefreshResult> RefreshResultAsync(string refreshToken) =>
        this.Service.RefreshAsync(refreshToken, ClientInfo.Unknown, CancellationToken.None);
}
