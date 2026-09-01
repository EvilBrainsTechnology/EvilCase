using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Json;

/// <summary>
/// Refuses an integer value for the enum, so a JSON body cannot address a member no name ever named.
/// </summary>
public sealed class StrictJsonStringEnumConverter<T> : JsonStringEnumConverter<T>
    where T : struct, Enum
{
    public StrictJsonStringEnumConverter()
        : base(namingPolicy: null, allowIntegerValues: false)
    { }
}
