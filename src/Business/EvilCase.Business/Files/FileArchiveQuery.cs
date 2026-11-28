using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileArchiveQuery
{
    public static IQueryable<FileVersion> OfActFiles(this IQueryable<FileVersion> versions, Guid actId)
    {
        return versions.Where(version => version.FileAsset!.ActId == actId);
    }

    public static IQueryable<FileVersion> OfFiles(this IQueryable<FileVersion> versions, IReadOnlyList<Guid> fileIds)
    {
        return versions.Where(version => fileIds.Contains(version.FileAssetId));
    }

    /// <summary>
    /// The files in the order of the file list, the versions of each file by number.
    /// </summary>
    public static IQueryable<FileVersion> InFileOrder(this IQueryable<FileVersion> versions)
    {
        return versions
            .OrderBy(static version => version.FileAsset!.Created)
            .ThenBy(static version => version.FileAssetId)
            .ThenBy(static version => version.Number);
    }

    public static IQueryable<ArchivedVersion> AsArchivedVersions(this IQueryable<FileVersion> versions)
    {
        return versions.Select(static version => new ArchivedVersion
        {
            FileId = version.FileAssetId,
            FileName = version.FileAsset!.FileName,
            Number = version.Number,
            Created = version.Created,
            StoragePath = version.StoragePath,
        });
    }
}
