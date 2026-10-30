namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// What a case's folder is named and placed by; the export holds every case this way at once.
/// </summary>
internal sealed record OutlinedCase
{
    public required Guid CaseId { get; init; }

    public required Guid? ParentCaseId { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }
}
