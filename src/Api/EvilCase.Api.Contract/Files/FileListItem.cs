namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileListItem
{
    public required Guid FileId { get; init; }

    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>
    /// When the current version was uploaded.
    /// </summary>
    public required DateTime Created { get; init; }

    /// <summary>
    /// The number of the file's current version, its highest.
    /// </summary>
    public required int Version { get; init; }

    public required int VersionCount { get; init; }

    /// <summary>
    /// The signature (p7s, p7m) that by name signs this file: the only file of the same case or act named this file's name
    /// and .p7s or .p7m, where no other file there carries this name. Set in a file list; the signers say whether it is
    /// detached.
    /// </summary>
    public Guid? SignatureFileId { get; init; }
}
