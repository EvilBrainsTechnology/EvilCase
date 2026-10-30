using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.ApiTokens;

namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

public sealed record ApiTokenCreateRequest
{
    [Required]
    [StringLength(ApiTokenLimits.MaxNameLength)]
    public required string Name { get; init; }

    public required ApiTokenScope Scope { get; init; }

    /// <summary>
    /// One of <see cref="ApiTokenLimits.LifetimeDays"/>.
    /// </summary>
    [AllowedValues(30, 90, 365)]
    public required int ExpiresInDays { get; init; }
}
