using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(TenantId), nameof(CaseNumber), IsUnique = true)]
[Index(nameof(ParentCaseId))]
[Index(nameof(ContactId))]
public sealed record Case : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public Guid? ParentCaseId { get; init; }

    public Guid? ContactId { get; init; }

    [MaxLength(64)]
    public required string CaseNumber { get; init; }

    [MaxLength(128)]
    public string? ExternalCaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? ParentCase { get; init; }

    public Contact? Contact { get; init; }

    public ICollection<Case> ChildCases { get; init; } = [];

    public ICollection<Act> Acts { get; init; } = [];

    public ICollection<Comment> Comments { get; init; } = [];

    public ICollection<FileAsset> Files { get; init; } = [];
}
