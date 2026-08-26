using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Files;

internal static class FileListQuery
{
    public static IQueryable<FileAsset> OfCase(this IQueryable<FileAsset> files, Guid caseId)
    {
        return files.Where(file => file.CaseId == caseId);
    }

    // Oldest first; the id breaks the tie two uploads in one transaction would leave.
    public static IQueryable<FileAsset> InUploadOrder(this IQueryable<FileAsset> files)
    {
        return files.OrderBy(file => file.Created).ThenBy(file => file.Id);
    }

    public static IQueryable<FileListItem> AsListItems(this IQueryable<FileAsset> files)
    {
        return files.Select(file => new FileListItem
        {
            FileId = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            Created = file.Created,
        });
    }
}
