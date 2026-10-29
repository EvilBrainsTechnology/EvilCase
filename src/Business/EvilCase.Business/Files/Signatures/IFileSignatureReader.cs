using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignatureReader
{
    /// <summary>
    /// The verdict on the ISDS seal of a stored file; the current version where none is named; null where the file or the version is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignatureResponse?> ReadFileSignature(Guid fileId, int? version, CancellationToken token);
}
