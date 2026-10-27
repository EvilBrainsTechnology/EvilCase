using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignatureState>))]
public enum FileSignatureState
{
    Valid = 0,

    Invalid = 1,

    /// <summary>
    /// The signature could not be judged: for a data message the revocation status could not be obtained, for a signed file
    /// the problem says why.
    /// </summary>
    Unverifiable = 2,
}
