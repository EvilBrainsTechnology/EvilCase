using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The counter behind one <c>{seq}</c>: the last value handed out in one series. It only counts up, so
/// two callers never take one value; a value taken inside a transaction that rolls back goes back with
/// it and the next caller takes it.
/// </summary>
[Index(nameof(OwnerId), nameof(Scope), IsUnique = true)]
public record NumberSequence : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long OwnerId { get; init; }

    /// <summary>
    /// What the series counts within — the kind of number, the case for an act number, and the period
    /// the pattern names.
    /// </summary>
    [MaxLength(128)]
    public required string Scope { get; init; }

    public required int LastValue { get; init; }

    public User? Owner { get; init; }
}
