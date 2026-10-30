using System.Diagnostics;
using EvilBrains.EvilCase.Domain.ApiTokens;

namespace EvilBrains.EvilCase.App.Models;

public static class ApiTokenDisplay
{
    public static string ScopeText(ApiTokenScope scope)
    {
        return scope switch
        {
            ApiTokenScope.ReadOnly => "Jen čtení",
            ApiTokenScope.ReadWrite => "Čtení i zápis",
            _ => throw new UnreachableException(),
        };
    }

    public static string ScopeHint(ApiTokenScope scope)
    {
        return scope switch
        {
            ApiTokenScope.ReadOnly => "Čte spisy, úkony, kontakty, štítky a soubory",
            ApiTokenScope.ReadWrite => "Navíc zakládá, mění a maže",
            _ => throw new UnreachableException(),
        };
    }

    public static string ScopeCss(ApiTokenScope scope)
    {
        return scope switch
        {
            ApiTokenScope.ReadOnly => "ec-token-chip ec-token-scope-read",
            ApiTokenScope.ReadWrite => "ec-token-chip ec-token-scope-write",
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// The day of a moment in the browser's time zone.
    /// </summary>
    public static string DayText(in DateTime moment)
    {
        return DateDisplay.Text(DateOnly.FromDateTime(moment.ToLocalTime()));
    }

    public static string LastUsedText(DateTime? lastUsed)
    {
        return lastUsed is { } moment ? MomentDisplay.Text(moment) : "Nepoužit";
    }
}
