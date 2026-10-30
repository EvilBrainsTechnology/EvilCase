using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.ApiTokens;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Outside the tenant like <see cref="RefreshToken"/>: authentication finds it before a tenant is known.
/// Revocation deletes the row.
/// </summary>
[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId))]
public sealed record ApiToken : IEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid UserId { get; init; }

    [MaxLength(64)]
    public required string Name { get; init; }

    public required ApiTokenScope Scope { get; init; }

    /// <summary>
    /// SHA-256 of the whole token, hex.
    /// </summary>
    [MaxLength(64)]
    public required string TokenHash { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public required DateTime Expires { get; init; }

    public DateTime? LastUsed { get; init; }

    public User? User { get; init; }
}
