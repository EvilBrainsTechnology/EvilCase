using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// What an upload stored and the file as it stands after it.
/// </summary>
public sealed record FileUploadResult
{
    public required FileListItem File { get; init; }

    public required FileUploadOutcome Outcome { get; init; }
}
