namespace EvilBrains.EvilCase.Api.Contract.Files;

public sealed record FileVersionItem
{
    public required int Number { get; init; }

    public required long SizeBytes { get; init; }

    public required DateTime Created { get; init; }

    public required string UploaderEmail { get; init; }
}
