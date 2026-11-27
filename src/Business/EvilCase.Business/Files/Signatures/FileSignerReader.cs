using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal sealed class FileSignerReader(IDbSession dbSession, IFileBlobStore blobStore) : IFileSignerReader
{
    public async Task<FileSignersResponse?> ListFileSigners(Guid fileId, int? version, CancellationToken token)
    {
        var fileVersion = await dbSession.Current.FileVersions.ReadFileVersion(fileId, version, token);
        if (fileVersion is null)
            return null;

        if (fileVersion.SizeBytes > FileLimits.MaxReadBytes)
            return new FileSignersResponse { Problem = FileSignersProblem.TooLarge, Signers = [] };

        var content = await blobStore.ReadFileBlobBytes(fileVersion.StoragePath, fileVersion.SizeBytes, token);
        if (content is null)
            return null;

        var response = CmsSigners.Read(content);
        if (response.Problem is not null || !DetachedSignatures.IsDetached(content))
            return response;

        var ownerFiles = await dbSession.Current.FileAssets
            .OfSameOwner(fileVersion.FileAsset!)
            .AsListItems()
            .ToListAsync(token);

        var (signedFile, problem) = DetachedSignatures.Pair(fileVersion.FileAssetId, ownerFiles);

        return response with { SignedFile = signedFile, SignedFileProblem = problem };
    }
}
