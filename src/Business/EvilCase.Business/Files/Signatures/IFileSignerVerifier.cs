using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

public interface IFileSignerVerifier
{
    /// <summary>
    /// The signers of a stored signed file with their verdicts; the current version where none is named; null where the file or the version is unknown or its blob is gone.
    /// </summary>
    public Task<FileSignersResponse?> VerifyFileSigners(Guid fileId, int? version, CancellationToken token);
}
