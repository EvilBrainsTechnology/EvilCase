using EvilBrains.EvilCase.Domain.Acts;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public static class ActDirectionDisplay
{
    public static string Text(ActDirection direction)
    {
        return direction switch
        {
            ActDirection.Incoming => "Příchozí",
            ActDirection.Outgoing => "Odchozí",
            _ => "",
        };
    }

    public static TablerColor Color(ActDirection direction)
    {
        return direction switch
        {
            ActDirection.Incoming => TablerColor.Blue,
            ActDirection.Outgoing => TablerColor.Green,
            _ => TablerColor.Default,
        };
    }
}
