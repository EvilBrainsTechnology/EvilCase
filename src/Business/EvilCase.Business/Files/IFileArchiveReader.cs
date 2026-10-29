namespace EvilBrains.EvilCase.Business.Files;

public interface IFileArchiveReader
{
    /// <summary>
    /// The case's own files, the current versions or all of them; null where the case is unknown.
    /// </summary>
    public Task<FileArchive?> ReadCaseFileArchive(Guid caseId, bool includeVersions, CancellationToken token);

    /// <summary>
    /// The act's files, the current versions or all of them; null where the act is unknown.
    /// </summary>
    public Task<FileArchive?> ReadActFileArchive(Guid actId, bool includeVersions, CancellationToken token);

    /// <summary>
    /// Streams the archive to the target, writing only asynchronously and holding no more than a few kilobytes. A blob
    /// that cannot be read throws, and what was written then lacks the archive's end.
    /// </summary>
    public Task WriteFileArchive(FileArchive archive, Stream target, CancellationToken token);
}
