using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.App.Components;

internal sealed class FilesStates(
    ICaseFilesClient caseFiles,
    IActFilesClient actFiles,
    IFileTransferClient fileTransfer,
    IFileVersionsClient fileVersions,
    IMainFileClient mainFiles) : IFilesStates
{
    public FilesCardState ForCase(Guid caseId)
    {
        return new FilesCardState(new CaseFileOwner(caseId, caseFiles, fileTransfer), fileTransfer, fileVersions, mainFiles);
    }

    public FilesCardState ForAct(Guid actId)
    {
        return new FilesCardState(new ActFileOwner(actId, actFiles, fileTransfer), fileTransfer, fileVersions, mainFiles);
    }
}
