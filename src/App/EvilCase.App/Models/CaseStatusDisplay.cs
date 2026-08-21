using EvilBrains.EvilCase.Domain.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public static class CaseStatusDisplay
{
    public static string Text(CaseStatus status) => status switch
    {
        CaseStatus.Active => "Aktivní",
        CaseStatus.WaitingOnAuthority => "Čeká na úřad",
        CaseStatus.Closed => "Uzavřený",
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
