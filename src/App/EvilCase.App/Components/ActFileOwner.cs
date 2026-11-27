using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

internal sealed class ActFileOwner(Guid actId, IActFilesClient actFiles, IFileTransferClient fileTransfer) : IFileOwner
{
    public async Task<IReadOnlyList<FileListItem>> ListFiles(CancellationToken token)
    {
        return (await actFiles.ListActFiles(actId, token)).Items;
    }

    public async Task<FileUploadResult> UploadFile(IBrowserFile file, CancellationToken token)
    {
        return await fileTransfer.UploadActFile(actId, file, token);
    }

    public async Task RenameFile(FileListItem file, string fileName, CancellationToken token)
    {
        await actFiles.RenameActFile(actId, file.FileId, new FileRenameRequest { FileName = fileName }, token);
    }

    public async Task DeleteFile(FileListItem file, CancellationToken token)
    {
        await actFiles.DeleteActFile(actId, file.FileId, token);
    }
}
