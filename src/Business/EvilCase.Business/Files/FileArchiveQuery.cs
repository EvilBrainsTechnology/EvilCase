using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileArchiveQuery
{
    /// <summary>
    /// The versions of the case's own files, never those of its sub-cases or its acts.
    /// </summary>
    public static IQueryable<FileVersion> OfCaseFiles(this IQueryable<FileVersion> versions, Guid caseId)
    {
        return versions.Where(version => version.FileAsset!.CaseId == caseId);
    }

    public static IQueryable<FileVersion> OfActFiles(this IQueryable<FileVersion> versions, Guid actId)
    {
        return versions.Where(version => version.FileAsset!.ActId == actId);
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
