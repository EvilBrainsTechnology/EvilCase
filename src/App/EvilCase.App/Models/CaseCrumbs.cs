using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.App.Models;

public static class CaseCrumbs
{
    /// <summary>
    /// Spisy, then a link to every case of the path.
    /// </summary>
    public static IReadOnlyList<EcCrumb> Through(IEnumerable<CaseCrumb> path)
    {
        return [new EcCrumb("Spisy", "/cases"), .. path.Select(static @case => new EcCrumb(@case.Title, $"/cases/{@case.CaseId}"))];
    }
}
