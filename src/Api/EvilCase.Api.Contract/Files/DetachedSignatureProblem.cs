using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Files;

[JsonConverter(typeof(StrictJsonStringEnumConverter<DetachedSignatureProblem>))]
public enum DetachedSignatureProblem
{
    /// <summary>
    /// No file of the case or act carries the signed name.
    /// </summary>
    Missing = 0,

    /// <summary>
    /// Two files carry the signed name, or two signatures name it.
    /// </summary>
    Ambiguous = 1,
}
