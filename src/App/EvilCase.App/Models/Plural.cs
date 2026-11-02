namespace EvilBrains.EvilCase.App.Models;

internal static class Plural
{
    /// <summary>
    /// The count with the Czech form of its noun: "1 úkon", "3 úkony", "0 úkonů", "23 úkonů".
    /// </summary>
    public static string Text(int count, string one, string few, string many)
    {
        return NumberDisplay.Text(count) + " " + Form(count, one, few, many);
    }

    /// <summary>
    /// The Czech form for a count: one for 1, few for 2 to 4, many for 0 and 5 and more.
    /// </summary>
    public static string Form(int count, string one, string few, string many)
    {
        return count switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
