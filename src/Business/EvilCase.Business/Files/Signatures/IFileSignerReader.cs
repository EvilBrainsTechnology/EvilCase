using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignerReader
{
    /// <summary>
    /// The signers of a stored signed file; null where the file is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignersResponse?> ListFileSigners(Guid fileId, CancellationToken token);
}
