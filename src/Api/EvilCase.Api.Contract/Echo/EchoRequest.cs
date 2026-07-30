namespace EvilBrains.EvilCase.Api.Contract.Echo;

public record EchoRequest
{
    public required string Message { get; init; }
}
