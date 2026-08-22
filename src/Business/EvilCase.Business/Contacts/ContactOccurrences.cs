using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Merges the three act occurrence sources into one display order. Pure — no <c>DbContext</c> — because
/// the sources come from three separate queries and cannot be ordered together in SQL.
/// </summary>
internal static class ContactOccurrences
{
    public static IReadOnlyList<ContactActOccurrence> InDisplayOrder(
        IEnumerable<ContactActOccurrence> issuedBy,
        IEnumerable<ContactActOccurrence> addressedTo,
        IEnumerable<ContactActOccurrence> numberIssuer)
    {
        return
        [
            .. issuedBy
                .Concat(addressedTo)
                .Concat(numberIssuer)
                .OrderByDescending(occurrence => occurrence.ActDate)
                .ThenByDescending(NumberOrder)
                .ThenByDescending(occurrence => occurrence.ActNumber, StringComparer.Ordinal)
                .ThenBy(occurrence => occurrence.Role),
        ];
    }

    // A number orders by what it says, not by how it reads: 1000 follows 999 instead of preceding 002.
    // A number written by hand outside the format (SDD-008) says nothing and falls back to its text.
    private static (DateOnly CaseDate, int CaseSequence, DateOnly ActDate, int ActSequence) NumberOrder(ContactActOccurrence occurrence)
    {
        var @case = CaseNumberFormat.ParseOrDefault(occurrence.CaseNumber);
        var act = ActNumberFormat.ParseOrDefault(occurrence.ActNumber);

        return (@case?.Date ?? DateOnly.MinValue, @case?.Sequence ?? 0, act?.Date ?? DateOnly.MinValue, act?.Sequence ?? 0);
    }
}
