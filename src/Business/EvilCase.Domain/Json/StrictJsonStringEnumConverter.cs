using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Json;

public sealed class StrictJsonStringEnumConverter<T> : JsonStringEnumConverter<T>
    where T : struct, Enum
{
    public StrictJsonStringEnumConverter()
        : base(namingPolicy: null, allowIntegerValues: false)
    { }
}
