namespace EvilBrains.EvilCase.Business.Export;

/// <summary>
/// What an act's folder is named and placed by; the export holds every act this way at once.
/// </summary>
internal sealed record OutlinedAct
{
    public required Guid ActId { get; init; }

    public required Guid CaseId { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }
}
