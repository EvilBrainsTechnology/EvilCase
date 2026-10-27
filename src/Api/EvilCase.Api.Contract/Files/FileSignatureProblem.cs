using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignatureProblem>))]
public enum FileSignatureProblem
{
    /// <summary>
    /// The file is no signed ISDS data message or delivery receipt.
    /// </summary>
    NotDataMessage = 0,

    TooLarge = 1,

    /// <summary>
    /// The content does not match the signature.
    /// </summary>
    ContentMismatch = 2,

    /// <summary>
    /// The certificate does not chain to a bundled PostSignum root.
    /// </summary>
    NotPostSignum = 3,

    /// <summary>
    /// The certificate belongs to someone other than the ISDS.
    /// </summary>
    NotIsdsSigner = 4,

    NotValidAtTimestamp = 5,

    /// <summary>
    /// The qualified timestamp is missing, does not cover the signature or does not chain to PostSignum.
    /// </summary>
    TimestampInvalid = 6,

    Revoked = 7,
}
