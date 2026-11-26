using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// What an upload stored and the file as it stands after it.
/// </summary>
public sealed record UploadedFile
{
    public required UploadKind Kind { get; init; }

    public required FileListItem File { get; init; }
}
