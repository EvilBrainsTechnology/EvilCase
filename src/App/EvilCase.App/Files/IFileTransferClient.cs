using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Files;

internal interface IFileTransferClient
{
    public Task<FileUploadResult> UploadCaseFile(Guid caseId, IBrowserFile file, CancellationToken token);

    public Task<FileUploadResult> UploadActFile(Guid actId, IBrowserFile file, CancellationToken token);

    public Task<FileUploadResult> UploadFileVersion(Guid fileId, IBrowserFile file, CancellationToken token);

    public Task<Stream> DownloadFileContent(Guid fileId, int? version, CancellationToken token);
}
