using System.Collections.Concurrent;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
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

    public async Task<FileSignatureResponse?> ReadFileSignature(Guid fileId, int? version, CancellationToken token)
    {
        // The tenant-filtered row comes first, so a cached verdict never reaches another tenant.
        var fileVersion = await dbSession.Current.FileVersions.ReadFileVersion(fileId, version, token);
        if (fileVersion is null)
            return null;

        if (cache.TryGetValue(CacheKey(fileVersion.Id), out FileSignatureResponse? cached))
            return cached;

        // Concurrent reads of one version share one verification; it outlives a caller that gives up.
        var flight = InFlight.GetOrAdd(
            fileVersion.Id,
            static (_, verification) => new Lazy<Task<FileSignatureResponse?>>(
                async () => await verification.Reader.VerifyFile(verification.FileVersion)),
            (Reader: this, FileVersion: fileVersion));

        return await flight.Value.WaitAsync(token);
    }

    private static (string, Guid) CacheKey(Guid fileVersionId)
    {
        return (nameof(FileSignatureReader), fileVersionId);
    }

    private async Task<FileSignatureResponse?> VerifyFile(FileVersion fileVersion)
    {
        try
        {
            var signature = await this.ReadAndVerify(fileVersion.StoragePath, fileVersion.SizeBytes);
            if (signature is null)
                return null;

            logger.LogInformation("Signature of file {FileAssetId} version {FileVersionNumber} verified as {SignatureState} {SignatureProblem}", fileVersion.FileAssetId, fileVersion.Number, signature.State, signature.Problem);

            TimeSpan? lifetime = signature.State switch
            {
                FileSignatureState.Valid => ValidLifetime,
                FileSignatureState.Invalid => InvalidLifetime,
                FileSignatureState.Unverifiable => null,
                _ => throw new UnreachableException(),
            };

            if (lifetime is { } cacheLifetime)
                cache.Set(CacheKey(fileVersion.Id), signature, cacheLifetime);

            return signature;
        }
        finally
        {
            // A finished verification leaves, so the next read of an unverifiable file asks again.
            InFlight.TryRemove(fileVersion.Id, out _);
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
