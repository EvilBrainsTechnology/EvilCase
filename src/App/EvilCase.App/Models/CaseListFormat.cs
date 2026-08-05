using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

public static class CaseListFormat
{
    private const string DateFormat = "d. M. yyyy";

    /// <summary>
    /// The change date, or the creation date where a case was never changed.
    /// </summary>
    public static string ChangedOn(CaseListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return (item.Updated ?? item.Created).ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The same date with the verb that belongs to it, for the card, which has no column header.
    /// </summary>
    public static string Changed(CaseListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return $"{(item.Updated is null ? "Založeno" : "Změněno")} {ChangedOn(item)}";
    }
}
