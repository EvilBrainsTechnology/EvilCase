using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.App.Models;

internal static class ActStatusRules
{
    public static ActStatus? Prefilled(ActDirection? direction, ActStatus? status)
    {
        return direction == ActDirection.Outgoing && status is null ? ActStatus.InProgress : status;
    }
}
