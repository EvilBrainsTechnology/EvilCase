using EvilBrains.EvilCase.Api.Contract.Files;
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

    public static IQueryable<FileListItem> AsListItems(this IQueryable<FileAsset> files)
    {
        return files.Select(static file => new FileListItem
        {
            FileId = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            Created = file.Created,
        });
    }
}
