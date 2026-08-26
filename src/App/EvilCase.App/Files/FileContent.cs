namespace EvilBrains.EvilCase.App.Files;

internal sealed record FileContent
{
    public required string MediaType { get; init; }

    public required Stream Content { get; init; }
}
