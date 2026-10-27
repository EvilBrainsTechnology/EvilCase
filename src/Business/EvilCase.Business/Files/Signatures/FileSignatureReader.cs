using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<Guid, Lazy<Task<FileSignatureResponse?>>> InFlight = new();

    public async Task<FileSignatureResponse?> ReadFileSignature(Guid fileId, CancellationToken token)
    {
        // The tenant-filtered row comes first, so a cached verdict never reaches another tenant.
        var file = await dbSession.Current.FileAssets
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return null;

        if (cache.TryGetValue(CacheKey(fileId), out FileSignatureResponse? cached))
            return cached;

        // Concurrent reads of one file share one verification; it outlives a caller that gives up.
        var flight = InFlight.GetOrAdd(
            fileId,
            static (id, verification) => new Lazy<Task<FileSignatureResponse?>>(
                async () => await verification.Reader.VerifyFile(id, verification.StoragePath, verification.SizeBytes)),
            (Reader: this, file.StoragePath, file.SizeBytes));

        return await flight.Value.WaitAsync(token);
    }

    private static (string, Guid) CacheKey(Guid fileId)
    {
        return (nameof(FileSignatureReader), fileId);
    }

    private async Task<FileSignatureResponse?> VerifyFile(Guid fileId, string storagePath, long sizeBytes)
    {
        try
        {
            var signature = await this.ReadAndVerify(storagePath, sizeBytes);
            if (signature is null)
                return null;

            logger.LogInformation("Signature of file {FileAssetId} verified as {SignatureState} {SignatureProblem}", fileId, signature.State, signature.Problem);

            TimeSpan? lifetime = signature.State switch
            {
                FileSignatureState.Valid => ValidLifetime,
                FileSignatureState.Invalid => InvalidLifetime,
                FileSignatureState.Unverifiable => null,
                _ => throw new UnreachableException(),
            };

            if (lifetime is { } cacheLifetime)
                cache.Set(CacheKey(fileId), signature, cacheLifetime);

            return signature;
        }
        finally
        {
            // A finished verification leaves, so the next read of an unverifiable file asks again.
            InFlight.TryRemove(fileId, out _);
        }
    }

    private async Task<FileSignatureResponse?> ReadAndVerify(string storagePath, long sizeBytes)
    {
        if (sizeBytes > FileLimits.MaxDataMessageBytes)
            return new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = FileSignatureProblem.TooLarge };

        await using var content = blobStore.ReadFileBlob(storagePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream(capacity: (int)sizeBytes);
        await content.CopyToAsync(buffer, CancellationToken.None);

        return DataMessageSignature.Verify(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), trust, revocation);
    }
}
