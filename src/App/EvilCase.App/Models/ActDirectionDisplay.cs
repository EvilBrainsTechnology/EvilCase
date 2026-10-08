using System.Diagnostics;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Icons;
using EvilBrains.EvilCase.Domain.Acts;

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

    /// <summary>
    /// Who sent an incoming act, who an outgoing one went to.
    /// </summary>
    public static string ContactLabel(ActDirection? direction)
    {
        return direction switch
        {
            null => "Kontakt",
            ActDirection.Incoming => "Odesílatel",
            ActDirection.Outgoing => "Adresát",
            _ => throw new UnreachableException(),
        };
    }

    public static string Icon(ActDirection direction)
    {
        return direction switch
        {
            ActDirection.Incoming => AppIcons.ArrowDownLeft,
            ActDirection.Outgoing => AppIcons.ArrowUpRight,
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// The name behind ec-timeline-dot-{name} and ec-timeline-direction-{name}.
    /// </summary>
    public static string Css(ActDirection direction)
    {
        return direction switch
        {
            ActDirection.Incoming => "incoming",
            ActDirection.Outgoing => "outgoing",
            _ => throw new UnreachableException(),
        };
    }

    public static EcBadgeTone Tone(ActDirection direction)
    {
        return direction switch
        {
            ActDirection.Incoming => EcBadgeTone.Incoming,
            ActDirection.Outgoing => EcBadgeTone.Outgoing,
            _ => throw new UnreachableException(),
        };
    }
}
