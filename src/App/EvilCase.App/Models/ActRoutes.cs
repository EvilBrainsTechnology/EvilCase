namespace EvilBrains.EvilCase.App.Models;

public static class ActRoutes
{
    public static string Detail(Guid actId)
    {
        return $"/acts/{actId}";
    }

    public static string Edit(Guid actId)
    {
        return $"/acts/{actId}/edit";
    }

    public static string New(Guid? caseId = null)
    {
        return caseId is { } id ? $"/acts/new?case={id}" : "/acts/new";
    }
}
