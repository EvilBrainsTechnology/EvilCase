using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The two patterns the application issues its own numbers from, one row for the whole application,
/// inserted by the migration and the operator's from then on. Changing a pattern rewrites no number
/// already issued. No owner: a tenant of its own (M8) needs one here, as <c>NumberSequences</c>
/// already carries one.
/// </summary>
public record NumberingSettings : IEntity
{
    [Key]
    public long Id { get; init; }

    [MaxLength(128)]
    public required string CaseNumberPattern { get; init; }

    [MaxLength(128)]
    public required string ActNumberPattern { get; init; }
}
