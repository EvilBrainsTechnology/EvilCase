using EvilBrains.EvilCase.Domain.Files;

namespace EvilBrains.EvilCase.Business.Files.Links;

public interface IFileLinkWriter
{
    /// <summary>
    /// A link for the caller's user and tenant, valid <see cref="FileLinkWriter.Lifetime"/>; refused where the target, or
    /// the version a download names, is unknown in the tenant.
    /// </summary>
    public Task<FileLinkCreateResult> CreateFileLink(FileLinkKind kind, Guid targetId, int? version, Guid apiTokenId, CancellationToken token);

    /// <summary>
    /// The link the secret names, deleted by the same statement that reads it.
    /// </summary>
    public Task<FileLinkConsumeResult> ConsumeFileLink(string secret, CancellationToken token);

    public Task DeleteExpiredFileLinks(CancellationToken token);
}
