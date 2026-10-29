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

        if (fileVersion.SizeBytes > FileLimits.MaxSignedFileBytes)
            return new FileSignersResponse { Problem = FileSignersProblem.TooLarge, Signers = [] };

        await using var content = blobStore.ReadFileBlob(fileVersion.StoragePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream(capacity: (int)fileVersion.SizeBytes);
        await content.CopyToAsync(buffer, token);

        var response = CmsSigners.Read(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (response.Problem is not null || !DetachedSignatures.IsDetached(buffer.GetBuffer().AsMemory(0, (int)buffer.Length)))
            return response;

        var ownerFiles = await dbSession.Current.FileAssets
            .OfSameOwner(fileVersion.FileAsset!)
            .AsListItems()
            .ToListAsync(token);

        var (signedFile, problem) = DetachedSignatures.Pair(fileVersion.FileAssetId, fileVersion.FileAsset!.FileName, ownerFiles);

        return response with { SignedFile = signedFile, SignedFileProblem = problem };
    }
}
