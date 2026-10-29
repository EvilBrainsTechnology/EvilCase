using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileVersionQuery
{
    public static IQueryable<FileVersion> OfFile(this IQueryable<FileVersion> versions, Guid fileId)
    {
        return versions.Where(version => version.FileAssetId == fileId);
    }

    /// <summary>
    /// The version a read names by number, or the file's current one where it names none, with its file; null where the
    /// file or the version is unknown.
    /// </summary>
    public static async Task<FileVersion?> ReadFileVersion(this IQueryable<FileVersion> versions, Guid fileId, int? version, CancellationToken token)
    {
        var ofFile = versions
            .Include(static fileVersion => fileVersion.FileAsset)
            .OfFile(fileId);

        if (version is { } number)
            return await ofFile.SingleOrDefaultAsync(fileVersion => fileVersion.Number == number, token);

        return await ofFile
            .OrderByDescending(static fileVersion => fileVersion.Number)
            .FirstOrDefaultAsync(token);
    }

    /// <summary>
    /// The join is what carries the e-mail — <see cref="FileVersion"/> has no <c>User</c> navigation.
    /// </summary>
    public static IQueryable<FileVersionItem> AsVersionItems(this IQueryable<FileVersion> versions, IQueryable<User> users)
    {
        return versions.Join(
            users,
            static version => version.UserId,
            static user => user.Id,
            static (version, user) => new FileVersionItem
            {
                Number = version.Number,
                SizeBytes = version.SizeBytes,
                Created = version.Created,
                UploaderEmail = user.Email,
            });
    }

    public static IQueryable<FileVersionItem> InNewestFirstOrder(this IQueryable<FileVersionItem> items)
    {
        return items.OrderByDescending(static item => item.Number);
    }
}
