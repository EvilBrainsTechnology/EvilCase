using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActStatusRequest
{
    /// <summary>
    /// Null takes the status off.
    /// </summary>
    public ActStatus? Status { get; init; }
}
