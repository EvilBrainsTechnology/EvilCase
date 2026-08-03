using EvilBrains.EvilCase.Api.Contract.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// How a case status reads and looks. One place, so a status added to the contract shows up as a missing
/// arm here rather than as three screens disagreeing.
/// </summary>
public static class CaseStatusDisplay
{
    public static string Text(CaseStatus status) => status switch
    {
        CaseStatus.Active => "Aktivní",
        CaseStatus.WaitingOnAuthority => "Čeká na úřad",
        CaseStatus.Closed => "Uzavřený",
        _ => "",
    };

    /// <summary>
    /// The filter's own wording. <c>Open</c> and <c>All</c> are not statuses and have no colour; the
    /// rest read the same as the status they name.
    /// </summary>
    public static string FilterText(CaseStatusFilter filter) => filter switch
    {
        CaseStatusFilter.Open => "Otevřené",
        CaseStatusFilter.All => "Všechny stavy",
        CaseStatusFilter.Active => Text(CaseStatus.Active),
        CaseStatusFilter.WaitingOnAuthority => Text(CaseStatus.WaitingOnAuthority),
        CaseStatusFilter.Closed => Text(CaseStatus.Closed),
        _ => "",
    };

    public static TablerColor Color(CaseStatus status) => status switch
    {
        CaseStatus.Active => TablerColor.Green,
        CaseStatus.WaitingOnAuthority => TablerColor.Yellow,
        CaseStatus.Closed => TablerColor.Secondary,
        _ => TablerColor.Default,
    };
}
