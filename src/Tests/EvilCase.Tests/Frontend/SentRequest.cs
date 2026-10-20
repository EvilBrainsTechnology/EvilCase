namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed record SentRequest
{
    public required IDictionary<string, object?> Options { get; init; }

    public required string? Body { get; init; }
}
