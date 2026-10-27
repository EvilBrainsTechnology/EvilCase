namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The verdict on the ISDS seal of a stored ZFO. Every time is UTC.
/// </summary>
public sealed record FileSignatureResponse
{
    public required FileSignatureState State { get; init; }

    /// <summary>
    /// Set where the state is <see cref="FileSignatureState.Invalid"/>.
    /// </summary>
    public FileSignatureProblem? Problem { get; init; }

    public string? CertificateIssuer { get; init; }

    public DateTime? CertificateValidFrom { get; init; }

    public DateTime? CertificateValidTo { get; init; }

    /// <summary>
    /// The qualified timestamp over the signature, the time the seal is judged at.
    /// </summary>
    public DateTime? Timestamped { get; init; }

    /// <summary>
    /// When the revocation status was obtained.
    /// </summary>
    public DateTime? RevocationChecked { get; init; }
}
