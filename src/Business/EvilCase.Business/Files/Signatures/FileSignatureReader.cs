using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal sealed class FileSignatureReader(
    IDbSession dbSession,
    IFileBlobStore blobStore,
    SignatureTrust trust,
    ISignatureRevocation revocation,
    IMemoryCache cache,
    ILogger<FileSignatureReader> logger) : IFileSignatureReader
{
    private static readonly TimeSpan ValidLifetime = TimeSpan.FromHours(1);

    private static readonly TimeSpan InvalidLifetime = TimeSpan.FromHours(24);

    public async Task<FileSignatureResponse?> ReadFileSignature(Guid fileId, CancellationToken token)
    {
        // The tenant-filtered row comes first, so a cached verdict never reaches another tenant.
        var file = await dbSession.Current.FileAssets
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return null;

        var cacheKey = (nameof(FileSignatureReader), fileId);
        if (cache.TryGetValue(cacheKey, out FileSignatureResponse? cached))
            return cached;

        FileSignatureResponse signature;

        if (file.SizeBytes > FileLimits.MaxDataMessageBytes)
        {
            signature = new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = FileSignatureProblem.TooLarge };
        }
        else
        {
            await using var content = blobStore.ReadFileBlob(file.StoragePath);
            if (content is null)
                return null;

            await using var buffer = new MemoryStream(capacity: (int)file.SizeBytes);
            await content.CopyToAsync(buffer, token);

            signature = DataMessageSignature.Verify(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), trust, revocation);
        }

        logger.LogInformation("Signature of file {FileAssetId} verified as {SignatureState} {SignatureProblem}", fileId, signature.State, signature.Problem);

        TimeSpan? lifetime = signature.State switch
        {
            FileSignatureState.Valid => ValidLifetime,
            FileSignatureState.Invalid => InvalidLifetime,
            FileSignatureState.Unverifiable => null,
            _ => throw new UnreachableException(),
        };

        if (lifetime is { } cacheLifetime)
            cache.Set(cacheKey, signature, cacheLifetime);

        return signature;
    }
}
