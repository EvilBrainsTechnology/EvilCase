using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.Domain.Cases;

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

    public static string Tile(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Active => "ec-stat-icon-active",
            CaseStatus.WaitingOnAuthority => "ec-stat-icon-waiting",
            CaseStatus.Closed => "ec-stat-icon-closed",
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

    public static string Dot(CaseStatus status)
    {
        return status switch
        {
            CaseStatus.Active => "ec-status-dot-active",
            CaseStatus.WaitingOnAuthority => "ec-status-dot-waiting",
            CaseStatus.Closed => "ec-status-dot-closed",
            _ => throw new UnreachableException(),
        };
    }
}
