using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignatureState>))]
public enum FileSignatureState
{
    Valid = 0,

    Invalid = 1,

    /// <summary>
    /// The signature checked out offline, but the revocation status could not be obtained.
    /// </summary>
    Unverifiable = 2,
}
