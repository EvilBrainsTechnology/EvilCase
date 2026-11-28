using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// An act of a case file tree with its files, in the order of its file list.
/// </summary>
public sealed record ActFileTree
{
    public required ActListItem Act { get; init; }

    public IReadOnlyList<FileListItem> Files { get; init; } = [];
}
