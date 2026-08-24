using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Merges the two act occurrence sources into one display order. Pure — no <c>DbContext</c> — because
/// the sources come from two separate queries and cannot be ordered together in SQL.
/// </summary>
internal static class ContactOccurrences
{
    public static IReadOnlyList<ContactActOccurrence> InDisplayOrder(
        IEnumerable<ContactActOccurrence> issuedBy,
        IEnumerable<ContactActOccurrence> addressedTo)
    {
        return issuedBy
            .Concat(addressedTo)
            .OrderByDescending(occurrence => occurrence.ActDate)
            .ThenByDescending(occurrence => occurrence.ActNumber.Length)
            .ThenByDescending(occurrence => occurrence.ActNumber, StringComparer.Ordinal)
            .AsReadOnlyList();
    }
}
