namespace EvilBrains.EvilCase.Api.Contract;

public record EchoResponse
{
    public required string Message { get; init; }
}
