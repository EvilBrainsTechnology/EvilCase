namespace EvilBrains.EvilCase.App.Files;

public sealed record DataMessageEvent(DateTimeOffset? Time, string Description);
