using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// The Czech strings of the case list, shared by the table and the cards so the two cannot drift.
/// </summary>
public static class CaseListFormat
{
    private const string DateFormat = "d. M. yyyy";

    /// <summary>
    /// The date the row is ordered by. A case that was never changed shows when it was founded rather
    /// than an empty column.
    /// </summary>
    public static string ChangedOn(CaseListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item.Updated ?? item.Created).ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The same date with the verb that belongs to it, for the card, which has no column header to say
    /// what the date is.
    /// </summary>
    public static string Changed(CaseListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return $"{(item.Updated is null ? "Založeno" : "Změněno")} {ChangedOn(item)}";
    }

    /// <summary>
    /// Czech counts one, a few and many differently, and getting it wrong is the first thing a native
    /// reader sees.
    /// </summary>
    public static string SubCases(int count) => count switch
    {
        1 => "1 podspis",
        2 or 3 or 4 => $"{count.ToString(CultureInfo.InvariantCulture)} podspisy",
        _ => $"{count.ToString(CultureInfo.InvariantCulture)} podspisů",
    };
}
