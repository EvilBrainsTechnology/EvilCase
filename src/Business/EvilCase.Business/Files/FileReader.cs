using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files.Signatures;
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

        var files = await context.FileAssets
            .OfCase(caseId)
            .InUploadOrder()
            .AsListItems()
            .ToListAsync(token);

        return DetachedSignatures.WithSignatureFiles(files);
    }

    public async Task<IReadOnlyList<FileListItem>?> ListActFiles(Guid actId, CancellationToken token)
    {
        var context = dbSession.Current;

        var actExists = await context.Acts.Exists(actId, token);
        if (!actExists)
            return null;

        var files = await context.FileAssets
            .OfAct(actId)
            .InUploadOrder()
            .AsListItems()
            .ToListAsync(token);

        return DetachedSignatures.WithSignatureFiles(files);
    }

    public async Task<FileDownload?> OpenFileContent(Guid fileId, int? version, CancellationToken token)
    {
        var fileVersion = await dbSession.Current.FileVersions.ReadFileVersion(fileId, version, token);
        if (fileVersion is null)
            return null;

        var content = blobStore.ReadFileBlob(fileVersion.StoragePath);
        if (content is null)
            return null;

        return new FileDownload
        {
            FileName = fileVersion.FileAsset!.FileName,
            MediaType = fileVersion.MediaType ?? DefaultMediaType,
            Content = content,
        };
    }

    public async Task<IReadOnlyList<FileVersionItem>?> ListFileVersions(Guid fileId, CancellationToken token)
    {
        var context = dbSession.Current;

        var fileExists = await context.FileAssets.Exists(fileId, token);
        if (!fileExists)
            return null;

        return await context.FileVersions
            .OfFile(fileId)
            .AsVersionItems(context.Users)
            .InNewestFirstOrder()
            .ToListAsync(token);
    }
}
