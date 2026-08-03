using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Parties;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// An authority, an official or a person. Reused across cases rather than owned by one, so a party
/// accumulates history across all of them.
/// </summary>
[Index(nameof(OwnerId))]
[Index(nameof(DataBoxId))]
public record Party : IEntity
{
    [Key]
    public long Id { get; init; }

    /// <summary>
    /// Present from this aggregate's first migration, before anything filters on it: until M8 a single
    /// user owns everything, and from M8 on every query and endpoint is scoped by this column.
    /// </summary>
    public required long OwnerId { get; init; }

    public required PartyKind Kind { get; init; }

    /// <summary>
    /// One field for every kind. A person is not split into given and family names, and an official's
    /// name is where the authority they act for is written.
    /// </summary>
    [MaxLength(256)]
    public required string Name { get; init; }

    /// <summary>
    /// One free-text block, held as it appears on the document the party sent, and printed back as a
    /// block. Nothing filters or sorts on any part of it.
    /// </summary>
    [MaxLength(1024)]
    public string? Address { get; init; }

    /// <summary>
    /// The ISDS identifier, seven characters today. Indexed because it is the one thing about a party
    /// that is unambiguous enough to look one up by.
    /// </summary>
    [MaxLength(16)]
    public string? DataBoxId { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public User? Owner { get; init; }

    public ICollection<CaseReference> AssignedCaseReferences { get; init; } = [];

    public ICollection<Act> IssuedActs { get; init; } = [];

    public ICollection<Act> AddressedActs { get; init; } = [];
}
