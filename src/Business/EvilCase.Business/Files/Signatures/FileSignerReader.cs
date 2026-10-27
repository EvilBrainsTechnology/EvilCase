using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal sealed class FileSignerReader(IDbSession dbSession, IFileBlobStore blobStore) : IFileSignerReader
{
    public async Task<FileSignersResponse?> ListFileSigners(Guid fileId, CancellationToken token)
    {
        var file = await dbSession.Current.FileAssets
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return null;

        if (file.SizeBytes > FileLimits.MaxSignedFileBytes)
            return new FileSignersResponse { Problem = FileSignersProblem.TooLarge, Signers = [] };

        await using var content = blobStore.ReadFileBlob(file.StoragePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream(capacity: (int)file.SizeBytes);
        await content.CopyToAsync(buffer, token);

        var response = CmsSigners.Read(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (response.Problem is not null || !DetachedSignatures.IsDetached(buffer.GetBuffer().AsMemory(0, (int)buffer.Length)))
            return response;

        var ownerFiles = await dbSession.Current.FileAssets
            .OfSameOwner(file)
            .AsListItems()
            .ToListAsync(token);

        var (signedFile, problem) = DetachedSignatures.Pair(file.Id, file.FileName, ownerFiles);

        return response with { SignedFile = signedFile, SignedFileProblem = problem };
    }
}
