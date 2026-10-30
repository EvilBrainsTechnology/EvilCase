using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.ApiTokens;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ApiTokenScope>))]
public enum ApiTokenScope
{
    ReadOnly = 0,

    ReadWrite = 1,
}
