using System.Collections.Concurrent;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal sealed class FileSignerVerifier(
    IDbSession dbSession,
    IFileBlobStore blobStore,
    [FromKeyedServices(SignatureTrust.QualifiedKey)] SignatureTrust trust,
    ISignatureRevocation revocation,
    TimeProvider timeProvider,
    IMemoryCache cache,
    ILogger<FileSignerVerifier> logger) : IFileSignerVerifier
{
    private static readonly TimeSpan ValidLifetime = TimeSpan.FromHours(1);

    private static readonly TimeSpan InvalidLifetime = TimeSpan.FromHours(24);

    private static readonly ConcurrentDictionary<(Guid, Guid?), Lazy<Task<FileSignersResponse?>>> InFlight = new();

    public async Task<FileSignersResponse?> VerifyFileSigners(Guid fileId, CancellationToken token)
    {
        var context = dbSession.Current;

        // The tenant-filtered row comes first, so a cached verdict never reaches another tenant.
        var file = await context.FileAssets
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return null;

        if (file.SizeBytes > FileLimits.MaxSignedFileBytes)
            return new FileSignersResponse { Problem = FileSignersProblem.TooLarge, Signers = [] };

        var ownerFiles = await context.FileAssets
            .OfSameOwner(file)
            .AsListItems()
            .ToListAsync(token);

        var (signedFile, pairingProblem) = DetachedSignatures.Pair(file.Id, file.FileName, ownerFiles);

        // A rename or an upload that pairs the signature with another file makes another key.
        var key = (file.Id, signedFile?.FileId);
        if (cache.TryGetValue(CacheKey(key), out FileSignersResponse? cached))
            return cached;

        var signedRow = signedFile is null
            ? null
            : await context.FileAssets
                .WithId(signedFile.FileId)
                .SingleOrDefaultAsync(token);

        if (signedFile is not null && signedRow is null)
            (signedFile, pairingProblem) = (null, DetachedSignatureProblem.Missing);

        // Concurrent reads of one pair share one verification; it outlives a caller that gives up.
        var flight = InFlight.GetOrAdd(
            key,
            static (pair, verification) => new Lazy<Task<FileSignersResponse?>>(
                async () => await verification.Verifier.VerifyFile(pair, verification.StoragePath, verification.SignedRow, verification.SignedFile, verification.PairingProblem)),
            (Verifier: this, file.StoragePath, SignedRow: signedRow, SignedFile: signedFile, PairingProblem: pairingProblem));

        return await flight.Value.WaitAsync(token);
    }

    private static (string, (Guid, Guid?)) CacheKey((Guid, Guid?) key)
    {
        return (nameof(FileSignerVerifier), key);
    }

    private async Task<FileSignersResponse?> VerifyFile(
        (Guid, Guid?) key,
        string storagePath,
        FileAsset? signedRow,
        FileListItem? signedFile,
        DetachedSignatureProblem? pairingProblem)
    {
        try
        {
            var response = await this.ReadAndVerify(storagePath, signedRow, signedFile, pairingProblem);
            if (response is null)
                return null;

            var verdicts = response.Problem?.ToString()
                ?? string.Join(", ", response.Signers.Select(static signer => signer.Problem is { } problem ? $"{signer.State} {problem}" : $"{signer.State}"));

            logger.LogInformation("Signers of file {FileAssetId} verified against file {SignedFileAssetId} as {SignerVerdicts}", key.Item1, key.Item2, verdicts);

            // An unverifiable signer is asked for again on the next read.
            if (!response.Signers.Any(static signer => signer.State == FileSignatureState.Unverifiable))
            {
                var lifetime = response.Signers.Any(static signer => signer.State == FileSignatureState.Valid) ? ValidLifetime : InvalidLifetime;
                cache.Set(CacheKey(key), response, lifetime);
            }

            return response;
        }
        finally
        {
            // A finished verification leaves, so the next read of an unverifiable file asks again.
            InFlight.TryRemove(key, out _);
        }
    }

    private async Task<FileSignersResponse?> ReadAndVerify(string storagePath, FileAsset? signedRow, FileListItem? signedFile, DetachedSignatureProblem? pairingProblem)
    {
        await using var content = blobStore.ReadFileBlob(storagePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, CancellationToken.None);

        var cms = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (!DetachedSignatures.IsDetached(cms))
            return SignedFileSignature.Verify(cms.Span, signedContent: null, trust, revocation, now);

        var listed = CmsSigners.Read(cms.Span);

        if (pairingProblem is { } problem)
        {
            var signerProblem = problem == DetachedSignatureProblem.Missing ? FileSignerProblem.SignedFileMissing : FileSignerProblem.SignedFileAmbiguous;

            return SignedFileSignature.Unverifiable(listed, signerProblem) with { SignedFileProblem = problem };
        }

        if (signedRow!.SizeBytes > FileLimits.MaxSignedFileBytes)
            return SignedFileSignature.Unverifiable(listed, FileSignerProblem.SignedFileTooLarge) with { SignedFile = signedFile };

        await using var signedContent = blobStore.ReadFileBlob(signedRow.StoragePath);
        if (signedContent is null)
            return SignedFileSignature.Unverifiable(listed, FileSignerProblem.SignedFileMissing) with { SignedFileProblem = DetachedSignatureProblem.Missing };

        await using var signedBuffer = new MemoryStream();
        await signedContent.CopyToAsync(signedBuffer, CancellationToken.None);

        return SignedFileSignature.Verify(cms.Span, signedBuffer.ToArray(), trust, revocation, now) with { SignedFile = signedFile };
    }
}
