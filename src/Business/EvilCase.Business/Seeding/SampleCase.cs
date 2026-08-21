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
    /// Set on a sub-case: the seeder gives it a submission and an answer with this contact. Null on the
    /// main case, whose acts are listed one by one.
    /// </summary>
    public string? CounterpartyKey { get; init; }

    public IReadOnlyList<SampleExternalNumber> ExternalNumbers { get; init; } = [];

    public IReadOnlyList<string> Comments { get; init; } = [];
}
