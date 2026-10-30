using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Files;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Outside the tenant like <see cref="ApiToken"/>: the anonymous request that presents it finds it before a tenant is
/// known. Its use deletes the row; the revocation of its token deletes it too.
/// </summary>
[Index(nameof(SecretHash), IsUnique = true)]
[Index(nameof(Expires))]
public sealed record FileLink : IEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>
    /// SHA-256 of the secret, hex.
    /// </summary>
    [MaxLength(64)]
    public required string SecretHash { get; init; }

    public required Guid TenantId { get; init; }

    public required Guid UserId { get; init; }

    public required Guid ApiTokenId { get; init; }

    public required FileLinkKind Kind { get; init; }

    /// <summary>
    /// The case, the act or the file <see cref="Kind"/> names.
    /// </summary>
    public required Guid TargetId { get; init; }

    /// <summary>
    /// The version a download names; the current one where it names none.
    /// </summary>
    public int? Version { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public required DateTime Expires { get; init; }
}
