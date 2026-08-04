using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The two patterns the application issues its own numbers from, one row for the whole application.
/// Changing a pattern rewrites no number already issued.
/// </summary>
public record NumberingSettings : IEntity
{
    /// <summary>
    /// The row seeded with the defaults, and the only one.
    /// </summary>
    public const long SingletonId = 1;

    [Key]
    public long Id { get; init; }

    [MaxLength(128)]
    public required string CaseNumberPattern { get; init; }

    [MaxLength(128)]
    public required string ActNumberPattern { get; init; }

    public DateTime? Updated { get; init; }
}
