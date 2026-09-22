using System.Diagnostics;
using EvilBrains.EvilCase.Domain.Acts;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public static class ActDirectionDisplay
{
    public static string Text(ActDirection? direction)
    {
        return direction switch
        {
            null => "—",
            ActDirection.Incoming => "Příchozí",
            ActDirection.Outgoing => "Odchozí",
            _ => throw new UnreachableException(),
        };
    }

    public static TablerColor Color(ActDirection? direction)
    {
        return direction switch
        {
            null => TablerColor.Default,
            ActDirection.Incoming => TablerColor.Blue,
            ActDirection.Outgoing => TablerColor.Green,
            _ => throw new UnreachableException(),
        };
    }
}
