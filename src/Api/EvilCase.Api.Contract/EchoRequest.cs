namespace EvilBrains.EvilCase.Api.Contract;

public record EchoRequest
{
    public required string Message { get; init; }
}
