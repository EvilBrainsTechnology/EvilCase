using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components.Forms;

namespace EvilBrains.EvilCase.App.Components;

/// <summary>
/// The case or the act whose files a files state holds: the file operations the API addresses through their owner.
/// </summary>
internal interface IFileOwner
{
    public Task<IReadOnlyList<FileListItem>> ListFiles(CancellationToken token);

    public Task<FileUploadResult> UploadFile(IBrowserFile file, CancellationToken token);

    public Task RenameFile(FileListItem file, string fileName, CancellationToken token);

    public Task DeleteFile(FileListItem file, CancellationToken token);
}
