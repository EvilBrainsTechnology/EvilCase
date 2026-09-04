using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

public sealed record UploadFileResult
{
    public required UploadFileOutcome Outcome { get; init; }

    public FileListItem? File { get; init; }
}
