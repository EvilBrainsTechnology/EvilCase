using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileListQuery
{
    public static IQueryable<FileAsset> OfCase(this IQueryable<FileAsset> files, Guid caseId)
    {
        return files.Where(file => file.CaseId == caseId);
    }

    public static IQueryable<FileAsset> OfAct(this IQueryable<FileAsset> files, Guid actId)
    {
        return files.Where(file => file.ActId == actId);
    }

    /// <summary>
    /// The files of the case or act the file belongs to, itself included.
    /// </summary>
    public static IQueryable<FileAsset> OfSameOwner(this IQueryable<FileAsset> files, FileAsset file)
    {
        return file.CaseId is { } caseId ? files.OfCase(caseId) : files.OfAct(file.ActId!.Value);
    }

    public static IQueryable<FileAsset> InUploadOrder(this IQueryable<FileAsset> files)
    {
        return files.OrderBy(static file => file.Created).ThenBy(static file => file.Id);
    }

    /// <summary>
    /// The files carrying the name, ignoring case, compared as the unique name indexes compare it.
    /// </summary>
    public static IQueryable<FileAsset> Named(this IQueryable<FileAsset> files, string fileName)
    {
        return files.Where(file => DatabaseFunctions.FileNameKey(file.FileName) == DatabaseFunctions.FileNameKey(fileName));
    }

    /// <summary>
    /// Each file once, carrying its current version.
    /// </summary>
    public static IQueryable<FileListItem> AsListItems(this IQueryable<FileAsset> files)
    {
        return files.Select(static file => new FileListItem
        {
            FileId = file.Id,
            FileName = file.FileName,
            SizeBytes = file.Versions
                .OrderByDescending(static version => version.Number)
                .Select(static version => version.SizeBytes)
                .First(),
            Created = file.Versions
                .OrderByDescending(static version => version.Number)
                .Select(static version => version.Created)
                .First(),
            Version = file.Versions.Max(static version => version.Number),
            VersionCount = file.Versions.Count,
        });
    }
}
