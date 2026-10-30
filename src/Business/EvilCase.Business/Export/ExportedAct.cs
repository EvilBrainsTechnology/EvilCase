using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// !ukon.json in the act's folder.
/// </summary>
internal sealed record ExportedAct
{
    public string Type { get; } = "act";

    public required Guid ActId { get; init; }

    public required string ActNumber { get; init; }

    public required string? ExternalActNumber { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public required string? Description { get; init; }

    public required ActDirection? Direction { get; init; }

    public required Guid CaseId { get; init; }

    public required ExportedContactReference? Contact { get; init; }

    public required IReadOnlyList<ExportedLabelReference> Labels { get; init; }

    public IReadOnlyList<ExportedComment> Comments { get; init; } = [];

    public IReadOnlyList<ExportedFile> Files { get; init; } = [];

    public required DateTime Created { get; init; }

    public required DateTime? Updated { get; init; }

    /// <summary>
    /// The act's folder in the archive.
    /// </summary>
    public string Path { get; init; } = "";
}
