using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

public static class ActStatusDisplay
{
    public static string Text(ActStatus status)
    {
        return status switch
        {
            ActStatus.InProgress => "Rozpracováno",
            ActStatus.Sent => "Odesláno",
            _ => throw new UnreachableException(),
        };
    }

    public static string FilterText(ActStatusFilter filter)
    {
        return filter switch
        {
            ActStatusFilter.All => "Všechny stavy",
            ActStatusFilter.None => "Bez stavu",
            ActStatusFilter.InProgress => Text(ActStatus.InProgress),
            ActStatusFilter.Sent => Text(ActStatus.Sent),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// The name behind the segment ids of the status field.
    /// </summary>
    public static string Css(ActStatus status)
    {
        return status switch
        {
            ActStatus.InProgress => "in-progress",
            ActStatus.Sent => "sent",
            _ => throw new UnreachableException(),
        };
    }

    public static EcBadgeTone Tone(ActStatus status)
    {
        return status switch
        {
            ActStatus.InProgress => EcBadgeTone.Waiting,
            ActStatus.Sent => EcBadgeTone.Closed,
            _ => throw new UnreachableException(),
        };
    }
}
