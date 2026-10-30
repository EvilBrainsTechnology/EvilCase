namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedFileVersion
{
    public required int Number { get; init; }

    public required long SizeBytes { get; init; }

    public required string? MediaType { get; init; }

    public required string Sha256 { get; init; }

    public required DateTime Created { get; init; }

    public required string UploaderEmail { get; init; }

    /// <summary>
    /// The version's entry in the archive; null where its blob is missing.
    /// </summary>
    public required string? Path { get; init; }

    public required bool Missing { get; init; }
}
