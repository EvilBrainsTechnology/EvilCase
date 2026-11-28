using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
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
    /// The cases above this one, the root first; empty for a root case.
    /// </summary>
    public IReadOnlyList<CaseCrumb> Ancestors { get; init; } = [];

    public IReadOnlyList<LabelItem> Labels { get; init; } = [];

    /// <summary>
    /// The files of the acts of this case and of every case below it.
    /// </summary>
    public int FileCount { get; init; }

    /// <summary>
    /// The acts holding the <see cref="FileCount"/> files.
    /// </summary>
    public int FileActCount { get; init; }
}
