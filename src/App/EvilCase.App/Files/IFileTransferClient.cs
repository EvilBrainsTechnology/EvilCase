using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Files;

internal interface IFileTransferClient
{
    public Task UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token);

    public Task<FileContent> DownloadFileContent(Guid fileId, CancellationToken token);
}
