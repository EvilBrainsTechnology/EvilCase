using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// Members 0–3 make a signature Invalid, the rest Unverifiable.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignerProblem>))]
public enum FileSignerProblem
{
    /// <summary>
    /// The content does not match the signature.
    /// </summary>
    ContentMismatch = 0,

    NotValidAtTimestamp = 1,

    /// <summary>
    /// The timestamp does not cover the signature or does not chain to a bundled authority.
    /// </summary>
    TimestampInvalid = 2,

    Revoked = 3,

    /// <summary>
    /// The certificate is no qualified certificate of a bundled authority.
    /// </summary>
    UntrustedIssuer = 4,

    /// <summary>
    /// Without a timestamp, the certificate is not valid today.
    /// </summary>
    NotValidNow = 5,

    CertificateMissing = 6,

    RevocationUnknown = 7,

    SignedFileMissing = 8,

    SignedFileAmbiguous = 9,

    SignedFileTooLarge = 10,

    /// <summary>
    /// The signer's signed signing-certificate attribute names another certificate or does not parse.
    /// </summary>
    SigningCertificateMismatch = 11,
}
