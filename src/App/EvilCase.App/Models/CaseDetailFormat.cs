using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

public static class CaseDetailFormat
{
    private const string MomentFormat = "d. M. yyyy H:mm";

    public static string Moment(in DateTime moment) =>
        moment.ToLocalTime().ToString(MomentFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// The change moment with the verb that belongs to it, or the creation one where nothing changed yet.
    /// </summary>
    public static string Changed(CaseDetailResponse detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        return $"{(detail.Updated is null ? "Založeno" : "Změněno")} {Moment(detail.Updated ?? detail.Created)}";
    }
}
