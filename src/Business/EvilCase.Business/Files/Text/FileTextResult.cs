using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Text;

public readonly union FileTextResult(FileTextPage, FileTextFailure, FileTextPageNotFound);
