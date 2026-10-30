namespace EvilBrains.EvilCase.Business.Files.Text;

public interface IFileTextReader
{
    /// <summary>
    /// One page of the text of a file version; the current version where none is named.
    /// </summary>
    public Task<FileTextResult> ReadFileText(Guid fileId, int? version, int page, CancellationToken token);
}
