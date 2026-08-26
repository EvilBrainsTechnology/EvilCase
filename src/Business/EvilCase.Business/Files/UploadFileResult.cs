using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

/// <summary>
/// What an upload produced: the file itself only where it was stored.
/// </summary>
public sealed record UploadFileResult
{
    public required UploadFileOutcome Outcome { get; init; }

    public FileListItem? File { get; init; }
}
