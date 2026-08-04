namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// One entry of a case's running diary.
/// </summary>
public sealed record CaseComment
{
    public required long Id { get; init; }

    public required string Body { get; init; }

    public required string AuthorEmail { get; init; }

    public required DateTime Created { get; init; }
}
