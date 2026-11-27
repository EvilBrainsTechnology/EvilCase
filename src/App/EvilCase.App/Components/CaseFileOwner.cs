using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

internal sealed class CaseFileOwner(Guid caseId, ICaseFilesClient caseFiles, IFileTransferClient fileTransfer) : IFileOwner
{
    public async Task<IReadOnlyList<FileListItem>> ListFiles(CancellationToken token)
    {
        return (await caseFiles.ListCaseFiles(caseId, token)).Items;
    }

    public async Task<FileUploadResult> UploadFile(IBrowserFile file, CancellationToken token)
    {
        return await fileTransfer.UploadCaseFile(caseId, file, token);
    }

    public async Task RenameFile(FileListItem file, string fileName, CancellationToken token)
    {
        await caseFiles.RenameCaseFile(caseId, file.FileId, new FileRenameRequest { FileName = fileName }, token);
    }

    public async Task DeleteFile(FileListItem file, CancellationToken token)
    {
        await caseFiles.DeleteCaseFile(caseId, file.FileId, token);
    }
}
