using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The unit of work inside a case, and the thing the user thinks in: one submission, decision, notice
/// or call.
/// </summary>
[Index(nameof(CaseId), nameof(Date))]
[Index(nameof(IssuedByPartyId))]
[Index(nameof(AddressedToPartyId))]
[Index(nameof(ExternalActNumber))]
public record Act : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long CaseId { get; init; }

    public required ActDirection Direction { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    /// <summary>
    /// The <em>číslo jednací</em> of whoever issued this one document. The mark of the whole proceeding
    /// is an <c>ExternalCaseNumber</c> instead.
    /// </summary>
    [MaxLength(128)]
    public string? ExternalActNumber { get; init; }

    /// <summary>
    /// When the act happened, and the only thing act lists sort by. A calendar date, not an instant —
    /// it starts a statutory period (M5) and the hour never enters that arithmetic.
    /// </summary>
    public required DateOnly Date { get; init; }

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

    public ICollection<Comment> Comments { get; init; } = [];
}
