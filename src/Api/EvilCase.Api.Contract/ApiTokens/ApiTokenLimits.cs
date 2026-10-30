namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

public static class ApiTokenLimits
{
    /// <summary>
    /// Mirrors <c>ApiToken.Name</c>'s <c>MaxLength</c>.
    /// </summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// Unexpired tokens one user may hold.
    /// </summary>
    public const int MaxActive = 20;

    public const int DefaultLifetimeDays = 90;

    /// <summary>
    /// <see cref="ApiTokenCreateRequest.ExpiresInDays"/> names the same values.
    /// </summary>
    public static readonly IReadOnlyList<int> LifetimeDays = [30, DefaultLifetimeDays, 365];
}
