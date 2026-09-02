using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// An authority, an official or a person. Shared across the tenant rather than owned by whoever typed it
/// in, so a contact accumulates history across every case it appears in.
/// </summary>
[Index(nameof(TenantId))]
public sealed record Contact : ITenantEntity, ISoftDeleteEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required ContactKind Kind { get; init; }

    /// <summary>
    /// One field for every kind. A person is not split into given and family names, and an official's
    /// name is where the authority they act for is written.
    /// </summary>
    [MaxLength(256)]
    public required string Name { get; init; }

    /// <summary>
    /// One free-text block, held as it appears on the document the contact sent, and printed back as a
    /// block. Nothing filters or sorts on any part of it.
    /// </summary>
    [MaxLength(1024)]
    public string? Address { get; init; }

    /// <summary>
    /// The ISDS identifier, seven characters today.
    /// </summary>
    [MaxLength(16)]
    public string? DataBoxId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public DateTime? Deleted { get; init; }

    public ICollection<ExternalCaseNumber> AssignedExternalCaseNumbers { get; init; } = [];

    public ICollection<ExternalActNumber> AssignedExternalActNumbers { get; init; } = [];

    public ICollection<Act> IssuedActs { get; init; } = [];

    public ICollection<Act> AddressedActs { get; init; } = [];
}
