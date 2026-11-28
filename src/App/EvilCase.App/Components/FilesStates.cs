using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.App.Components;

internal sealed class FilesStates(
    IActFilesClient actFiles,
    IFilesClient files,
    IFileTransferClient fileTransfer,
    IFileVersionsClient fileVersions,
    IMainFileClient mainFiles) : IFilesStates
{
    public FilesCardState ForAct(Guid actId)
    {
        return new FilesCardState(actId, actFiles, files, fileTransfer, fileVersions, mainFiles);
    }
}
