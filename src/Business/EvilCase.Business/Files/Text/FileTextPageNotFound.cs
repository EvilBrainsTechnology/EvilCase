namespace EvilBrains.EvilCase.Business.Files.Text;

public sealed record FileTextPageNotFound
{
    public required int PageCount { get; init; }
}
