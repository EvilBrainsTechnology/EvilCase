using System.Buffers.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Files;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Links;

/// <summary>
/// The secret is 256 random bits stored as SHA-256, like an API token. Nothing here logs it.
/// </summary>
internal sealed class FileLinkWriter(IDbSession dbSession, IUserContext userContext, TimeProvider timeProvider, ILogger<FileLinkWriter> logger)
    : IFileLinkWriter
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private const int SecretSizeInBytes = 32;

    public async Task<FileLinkCreateResult> CreateFileLink(FileLinkKind kind, Guid targetId, int? version, Guid apiTokenId, CancellationToken token)
    {
        var context = dbSession.Current;

        var targetExists = kind switch
        {
            FileLinkKind.Download => await context.FileVersions.ReadFileVersion(targetId, version, token) is not null,
            FileLinkKind.CaseUpload => await context.Cases.Exists(targetId, token),
            FileLinkKind.ActUpload => await context.Acts.Exists(targetId, token),
            FileLinkKind.VersionUpload => await context.FileAssets.Exists(targetId, token),
            _ => throw new UnreachableException(),
        };

        if (!targetExists)
            return new FileLinkCreateResult { Outcome = FileLinkCreateOutcome.TargetNotFound };

        var secret = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SecretSizeInBytes));
        var expires = timeProvider.GetUtcNow().UtcDateTime + Lifetime;

        var link = new FileLink
        {
            SecretHash = Hash(secret),
            TenantId = userContext.TenantId,
            UserId = userContext.UserId,
            ApiTokenId = apiTokenId,
            Kind = kind,
            TargetId = targetId,
            Version = version,
            Expires = expires,
        };

        context.FileLinks.Add(link);
        await context.SaveChangesAsync(token);

        logger.LogInformation("File link {FileLinkId} ({FileLinkKind}) was created for {TargetId}", link.Id, kind, targetId);

        return new FileLinkCreateResult { Outcome = FileLinkCreateOutcome.Created, Secret = secret, Expires = expires };
    }

    public async Task<FileLinkConsumeResult> ConsumeFileLink(string secret, CancellationToken token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // One statement finds and spends the link, so two requests presenting it at once cannot both get it.
        var consumed = await dbSession.Current.FileLinks
            .FromSql($"""DELETE FROM "FileLinks" WHERE "SecretHash" = {Hash(secret)} RETURNING *""")
            .AsNoTracking()
            .ToListAsync(token);

        if (consumed.SingleOrDefault() is not { } link || link.Expires <= now)
            return new FileLinkConsumeResult { Outcome = FileLinkConsumeOutcome.NotFound };

        logger.LogInformation("File link {FileLinkId} ({FileLinkKind}) was used for {TargetId}", link.Id, link.Kind, link.TargetId);

        return new FileLinkConsumeResult
        {
            Outcome = FileLinkConsumeOutcome.Consumed,
            Link = new ConsumedFileLink
            {
                TenantId = link.TenantId,
                UserId = link.UserId,
                Kind = link.Kind,
                TargetId = link.TargetId,
                Version = link.Version,
            },
        };
    }

    public async Task DeleteExpiredFileLinks(CancellationToken token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await dbSession.Current.FileLinks
            .Where(link => link.Expires <= now)
            .ExecuteDeleteAsync(token);
    }

    private static string Hash(string secret)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }
}
