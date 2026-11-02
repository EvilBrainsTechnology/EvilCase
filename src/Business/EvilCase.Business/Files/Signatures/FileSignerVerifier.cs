using EvilBrains.EvilCase.Api.Contract.Files;
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

    public async Task<FileSignersResponse?> VerifyFileSigners(Guid fileId, int? version, CancellationToken token)
    {
        var context = dbSession.Current;

        // The tenant-filtered row comes first, so a cached verdict never reaches another tenant.
        var fileVersion = await context.FileVersions.ReadFileVersion(fileId, version, token);
        if (fileVersion is null)
            return null;

        if (fileVersion.SizeBytes > FileLimits.MaxReadBytes)
            return new FileSignersResponse { Problem = FileSignersProblem.TooLarge, Signers = [] };

        var file = fileVersion.FileAsset!;

        var ownerFiles = await context.FileAssets
            .OfSameOwner(file)
            .AsListItems()
            .ToListAsync(token);

        var (signedFile, pairingProblem) = DetachedSignatures.Pair(file.Id, file.FileName, ownerFiles);

        // A detached signature is verified against the current version of the file it signs.
        var signedVersion = signedFile is null
            ? null
            : await context.FileVersions.ReadFileVersion(signedFile.FileId, version: null, token);

        if (signedFile is not null && signedVersion is null)
            (signedFile, pairingProblem) = (null, DetachedSignatureProblem.Missing);

        // A rename, an upload or a new version that pairs the signature with other content makes another key.
        var key = (fileVersion.Id, signedVersion?.Id);
        if (cache.TryGetValue(CacheKey(key), out FileSignersResponse? cached))
            return cached;

        var response = await this.ReadAndVerify(fileVersion.StoragePath, signedVersion, signedFile, pairingProblem, token);
        if (response is null)
            return null;

        var verdicts = response.Problem?.ToString()
            ?? string.Join(", ", response.Signers.Select(static signer => signer.Problem is { } problem ? $"{signer.State} {problem}" : $"{signer.State}"));

        logger.LogInformation("Signers of file {FileAssetId} verified against file {SignedFileAssetId} as {SignerVerdicts}", file.Id, signedFile?.FileId, verdicts);

        // An unverifiable signer is asked for again on the next read.
        if (!response.Signers.Any(static signer => signer.State == FileSignatureState.Unverifiable))
        {
            var lifetime = response.Signers.Any(static signer => signer.State == FileSignatureState.Valid) ? ValidLifetime : InvalidLifetime;
            cache.Set(CacheKey(key), response, lifetime);
        }

        return response;
    }

    private static (string, (Guid, Guid?)) CacheKey((Guid, Guid?) key)
    {
        return (nameof(FileSignerVerifier), key);
    }

    private async Task<FileSignersResponse?> ReadAndVerify(
        string storagePath, FileVersion? signedVersion, FileListItem? signedFile, DetachedSignatureProblem? pairingProblem, CancellationToken token)
    {
        await using var content = blobStore.ReadFileBlob(storagePath);
        if (content is null)
            return null;

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, token);

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

        if (signedVersion!.SizeBytes > FileLimits.MaxReadBytes)
            return SignedFileSignature.Unverifiable(listed, FileSignerProblem.SignedFileTooLarge) with { SignedFile = signedFile };

        await using var signedContent = blobStore.ReadFileBlob(signedVersion.StoragePath);
        if (signedContent is null)
            return SignedFileSignature.Unverifiable(listed, FileSignerProblem.SignedFileMissing) with { SignedFileProblem = DetachedSignatureProblem.Missing };

        await using var signedBuffer = new MemoryStream();
        await signedContent.CopyToAsync(signedBuffer, token);

        return SignedFileSignature.Verify(cms.Span, signedBuffer.ToArray(), trust, revocation, now) with { SignedFile = signedFile };
    }
}
