namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// One signer of a CMS signature as the file states it, with the verdict where verified. Every time is UTC.
/// </summary>
public sealed record FileSigner
{
    /// <summary>
    /// The certificate subject's common name; empty where the file does not carry the certificate.
    /// </summary>
    public required string Name { get; init; }

    public required FileSignerKind Kind { get; init; }

    /// <summary>
    /// The issuing authority's short name for PostSignum, I.CA and eIdentity, its common name otherwise.
    /// </summary>
    public string? CertificateIssuer { get; init; }

    public DateTime? CertificateValidFrom { get; init; }

    public DateTime? CertificateValidTo { get; init; }

    /// <summary>
    /// The time of the RFC 3161 timestamp token over the signature.
    /// </summary>
    public DateTime? Timestamped { get; init; }

    /// <summary>
    /// The signing time the signer states in a signed attribute.
    /// </summary>
    public DateTime? SigningTime { get; init; }

    /// <summary>
    /// The verdict; null in a listing that does not verify.
    /// </summary>
    public FileSignatureState? State { get; init; }

    /// <summary>
    /// Set where the state is Invalid or Unverifiable.
    /// </summary>
    public FileSignerProblem? Problem { get; init; }

    /// <summary>
    /// When the revocation status was obtained; set where the state is Valid.
    /// </summary>
    public DateTime? RevocationChecked { get; init; }
}
