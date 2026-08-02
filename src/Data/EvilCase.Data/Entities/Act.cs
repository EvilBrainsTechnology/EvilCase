using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The unit of work inside a case, and the thing the user thinks in: one submission, decision, notice
/// or call.
/// </summary>
[Index(nameof(CaseId))]
[Index(nameof(IssuedByPartyId))]
[Index(nameof(AddressedToPartyId))]
[Index(nameof(FileNumber))]
public record Act : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long CaseId { get; init; }

    /// <summary>
    /// The act's number within its case. Deliberately not unique: a real case file has two unrelated
    /// submissions filed under one number, so this orders acts and does not identify them.
    /// </summary>
    public required int Ordinal { get; init; }

    public required ActDirection Direction { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    /// <summary>
    /// The <em>číslo jednací</em> of whoever issued this one document. The mark of the whole proceeding
    /// is a <c>CaseReference</c> instead.
    /// </summary>
    [MaxLength(128)]
    public string? FileNumber { get; init; }

    /// <summary>
    /// Calendar dates, not instants — a delivery date starts a statutory period (M5) and the hour it
    /// happened never enters that arithmetic. Which of the four apply depends on the direction: an
    /// outgoing act is drafted, sent and delivered; an incoming one is received.
    /// </summary>
    public DateOnly? Drafted { get; init; }

    /// <inheritdoc cref="Drafted"/>
    public DateOnly? Sent { get; init; }

    /// <inheritdoc cref="Drafted"/>
    public DateOnly? Delivered { get; init; }

    /// <inheritdoc cref="Drafted"/>
    public DateOnly? Received { get; init; }

    /// <summary>
    /// What was said in this act. Unbounded, and it lives here and nowhere else — a file asset is
    /// shared by every link that points at it, while a summary is about one act.
    /// </summary>
    public string? Summary { get; init; }

    public long? IssuedByPartyId { get; init; }

    public long? AddressedToPartyId { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Party? IssuedBy { get; init; }

    public Party? AddressedTo { get; init; }

    public ICollection<ActFileLink> Files { get; init; } = [];

    public ICollection<ActFileLink> AttachmentsTakenFromIt { get; init; } = [];

    /// <inheritdoc cref="Files"/>
    public ICollection<Comment> Comments { get; init; } = [];
}
