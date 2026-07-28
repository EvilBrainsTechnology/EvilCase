namespace EvilBrains.EvilCase.Api.Client;

public record EchoRequest
{
    public required string Message { get; init; }
}
