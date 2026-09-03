using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed record SampleCase
{
    public required string Key { get; init; }

    public string? ParentKey { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public required DateOnly Date { get; init; }

    /// <summary>
    /// The case's contact. On a sub-case the seeder also gives it a submission and an answer with it.
    /// </summary>
    public required string CounterpartyKey { get; init; }

    public string? ExternalCaseNumber { get; init; }

    public IReadOnlyList<string> Comments { get; init; } = [];
}
