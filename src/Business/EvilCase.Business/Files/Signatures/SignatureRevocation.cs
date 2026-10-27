namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal sealed record SignatureRevocation
{
    public required SignatureRevocationStatus Status { get; init; }

    /// <summary>
    /// When the status was asked for, UTC.
    /// </summary>
    public required DateTime Checked { get; init; }
}
