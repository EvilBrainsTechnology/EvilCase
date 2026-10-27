using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<FileSignersProblem>))]
public enum FileSignersProblem
{
    /// <summary>
    /// The file is no CMS SignedData.
    /// </summary>
    NotSigned = 0,

    TooLarge = 1,

    TooManySigners = 2,
}
