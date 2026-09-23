using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.Domain.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public static class CaseStatusDisplay
{
    public static string Text(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Active => "Aktivní",
            CaseStatus.WaitingOnAuthority => "Čeká na úřad",
            CaseStatus.Closed => "Uzavřený",
            _ => throw new UnreachableException(),
        };
    }

    public static string FilterText(CaseStatusFilter filter)
    {
        return filter switch
        {
            CaseStatusFilter.Open => "Otevřené",
            CaseStatusFilter.All => "Všechny stavy",
            CaseStatusFilter.Active => Text(CaseStatus.Active),
            CaseStatusFilter.WaitingOnAuthority => Text(CaseStatus.WaitingOnAuthority),
            CaseStatusFilter.Closed => Text(CaseStatus.Closed),
            _ => throw new UnreachableException(),
        };
    }

    public static TablerColor Color(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Active => TablerColor.Green,
            CaseStatus.WaitingOnAuthority => TablerColor.Yellow,
            CaseStatus.Closed => TablerColor.Secondary,
            _ => throw new UnreachableException(),
        };
    }

    public static EcBadgeTone Tone(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Active => EcBadgeTone.Active,
            CaseStatus.WaitingOnAuthority => EcBadgeTone.Waiting,
            CaseStatus.Closed => EcBadgeTone.Closed,
            _ => throw new UnreachableException(),
        };
    }
}
