using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignerKind>))]
public enum FileSignerKind
{
    /// <summary>
    /// ETSI QCP-n-qscd: a natural person's qualified certificate, the key on a qualified device.
    /// </summary>
    QualifiedSignature = 0,

    /// <summary>
    /// ETSI QCP-n: a natural person's qualified certificate, the key elsewhere.
    /// </summary>
    QualifiedCertificateSignature = 1,

    /// <summary>
    /// ETSI QCP-l-qscd: a legal person's qualified certificate, the key on a qualified device.
    /// </summary>
    QualifiedSeal = 2,

    /// <summary>
    /// ETSI QCP-l: a legal person's qualified certificate, the key elsewhere.
    /// </summary>
    QualifiedCertificateSeal = 3,

    Other = 4,
}
