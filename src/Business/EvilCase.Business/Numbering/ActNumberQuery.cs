using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

public static class ActNumberQuery
{
    private const string Escape = "\\";

    /// <summary>The acts whose own number belongs to the case number and the day.</summary>
    public static IQueryable<Act> WithNumberOfDay(this IQueryable<Act> acts, string caseNumber, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(acts);

        // A hand-written case number stands in the prefix literally and can carry a wildcard.
        var pattern = EscapeWildcards(ActNumberFormat.DayPrefix(caseNumber, date)) + "%";

        return acts.Where(act => EF.Functions.Like(act.ActNumber, pattern, Escape));
    }

    private static string EscapeWildcards(string text) => text
        .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
        .Replace("%", Escape + "%", StringComparison.Ordinal)
        .Replace("_", Escape + "_", StringComparison.Ordinal);
}
