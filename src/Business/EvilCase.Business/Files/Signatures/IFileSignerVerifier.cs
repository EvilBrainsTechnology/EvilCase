using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignerVerifier
{
    /// <summary>
    /// The signers of a stored signed file with their verdicts; null where the file is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignersResponse?> VerifyFileSigners(Guid fileId, CancellationToken token);
}
