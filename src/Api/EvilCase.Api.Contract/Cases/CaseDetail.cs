using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseDetail
{
    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public string? ExternalCaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public ContactListItem? Contact { get; init; }

    public CaseListItem? ParentCase { get; init; }

    /// <summary>
    /// Direct children only, never descendants.
    /// </summary>
    public IReadOnlyList<CaseListItem> ChildCases { get; init; } = [];
}
