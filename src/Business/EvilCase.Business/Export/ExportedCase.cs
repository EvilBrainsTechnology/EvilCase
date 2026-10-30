using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// !spis.json in the case's folder.
/// </summary>
internal sealed record ExportedCase
{
    public string Type { get; } = "case";

    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string? ExternalCaseNumber { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public required string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public required Guid? ParentCaseId { get; init; }

    public required ExportedContactReference? Contact { get; init; }

    public required IReadOnlyList<ExportedLabelReference> Labels { get; init; }

    public IReadOnlyList<ExportedComment> Comments { get; init; } = [];

    public IReadOnlyList<ExportedFile> Files { get; init; } = [];

    public required DateTime Created { get; init; }

    public required DateTime? Updated { get; init; }

    /// <summary>
    /// The case's folder in the archive.
    /// </summary>
    public string Path { get; init; } = "";
}
