using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// A file of a list over several acts, with the act it belongs to.
/// </summary>
internal sealed record ActFile
{
    public required Guid ActId { get; init; }

    public required FileListItem File { get; init; }
}
