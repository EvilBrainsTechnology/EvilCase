using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignerReader
{
    /// <summary>
    /// The signers of a stored signed file; the current version where none is named; null where the file or the version is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignersResponse?> ListFileSigners(Guid fileId, int? version, CancellationToken token);
}
