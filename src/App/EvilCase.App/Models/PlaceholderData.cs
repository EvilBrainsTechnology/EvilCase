using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.App.Models;

// Hard-coded placeholder content for the dashboard skeleton; replaced once the API provides real data.
public static class SampleData
{
    public static IReadOnlyList<CaseSummary> RecentCases { get; } =
    [
        new("12 C 148/2026", "Vymáhání pohledávky", "Novák Trading s.r.o.", CaseStatus.Active, Today.AddDays(-1)),
        new("8 Cm 42/2026", "Spor o smluvní pokutu", "Bezuchova a.s.", CaseStatus.Active, Today.AddDays(-3)),
        new("21 T 9/2026", "Zastoupení poškozeného", "Marta Dvořáková", CaseStatus.WaitingOnAuthority, Today.AddDays(-6)),
        new("3 Nc 511/2026", "Předběžné opatření", "Klára Šimková", CaseStatus.Active, Today.AddDays(-9)),
        new("15 C 77/2025", "Náhrada škody", "Statek Podolí s.r.o.", CaseStatus.Closed, Today.AddDays(-24)),
    ];

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
}
