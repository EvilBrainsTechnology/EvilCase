using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files;

public sealed record FileVersionUploadResult
{
    public required FileVersionUploadOutcome Outcome { get; init; }

    public FileListItem? File { get; init; }
}
