using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Files;

internal sealed class FileReader(IDbSession dbSession, IFileBlobStore blobStore) : IFileReader
{
    private const string DefaultMediaType = "application/octet-stream";

    public async Task<IReadOnlyList<FileListItem>?> ListCaseFiles(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        var caseExists = await context.Cases.Exists(caseId, token);
        if (!caseExists)
            return null;

        return await context.FileAssets
            .OfCase(caseId)
            .InUploadOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid caseId, Guid actId, CancellationToken token)
    {
        var context = dbSession.Current;

        var actExists = await context.Acts.OfCase(caseId).Exists(actId, token);
        if (!actExists)
            return null;

        return await context.FileAssets
            .OfAct(caseId, actId)
            .InUploadOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<FileDownload?> OpenFileContent(Guid fileId, CancellationToken token)
    {
        var file = await dbSession.Current.FileAssets
            .WithId(fileId)
            .SingleOrDefaultAsync(token);

        if (file is null)
            return null;

        var content = blobStore.ReadFileBlob(file.StoragePath);
        if (content is null)
            return null;

        return new FileDownload
        {
            FileName = file.FileName,
            MediaType = file.MediaType ?? DefaultMediaType,
            Content = content,
        };
    }
}
