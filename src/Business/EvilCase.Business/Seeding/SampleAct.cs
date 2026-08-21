using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed record SampleAct
{
    public required ActDirection Direction { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required DateOnly Date { get; init; }

    public required string CounterpartyKey { get; init; }

    public IReadOnlyList<SampleExternalNumber> ExternalNumbers { get; init; } = [];

    public IReadOnlyList<string> Comments { get; init; } = [];

    /// <summary>
    /// Names a second TXT beside the act's own, for the act that carries an evidence bundle.
    /// </summary>
    public string? ExtraFileSuffix { get; init; }
}
