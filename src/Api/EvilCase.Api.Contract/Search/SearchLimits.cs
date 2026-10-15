namespace EvilBrains.EvilCase.Api.Contract.Search;

public static class SearchLimits
{
    public const int TermMinLength = 2;

    public const int TermMaxLength = 256;

    /// <summary>
    /// The rows one group of the search answer carries.
    /// </summary>
    public const int GroupSize = 3;
}
