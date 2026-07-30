namespace EvilBrains.EvilCase.Api.Contract.Echo;

public record EchoResponse
{
    public required string Message { get; init; }
}
