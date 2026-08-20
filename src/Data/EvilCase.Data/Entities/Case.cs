using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A proceeding. An optional parent gives it its place in a hierarchy (SDD-009).
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(TenantId), nameof(CaseNumber), IsUnique = true)]
[Index(nameof(TenantId), nameof(Date))]
[Index(nameof(ParentCaseId))]
public record Case : ITenantEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    public required Guid UserId { get; init; }

    public Guid? ParentCaseId { get; init; }

    [MaxLength(64)]
    public required string CaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    [MaxLength(4000)]
    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? ParentCase { get; init; }

    public ICollection<Case> ChildCases { get; init; } = [];

    public ICollection<ExternalCaseNumber> ExternalCaseNumbers { get; init; } = [];

    public ICollection<Act> Acts { get; init; } = [];

    public ICollection<Comment> Comments { get; init; } = [];

    public ICollection<FileAsset> Files { get; init; } = [];
}
