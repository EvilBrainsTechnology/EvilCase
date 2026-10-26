namespace EvilBrains.EvilCase.App.Files;

internal sealed record DataMessageEvent(DateTimeOffset? Time, string Description);
