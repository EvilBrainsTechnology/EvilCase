using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignatureReader
{
    /// <summary>
    /// The verdict on the ISDS seal of a stored file; null where the file is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignatureResponse?> ReadFileSignature(Guid fileId, CancellationToken token);
}
