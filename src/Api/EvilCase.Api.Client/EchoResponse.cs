namespace EvilBrains.EvilCase.Api.Client;

public record EchoResponse
{
    public required string Message { get; init; }
}
