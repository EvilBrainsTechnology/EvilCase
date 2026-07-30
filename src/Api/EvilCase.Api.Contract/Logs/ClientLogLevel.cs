using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Logs;

[JsonConverter(typeof(JsonStringEnumConverter<ClientLogLevel>))]
public enum ClientLogLevel
{
    Verbose = 0,

    Debug = 1,

    Information = 2,

    Warning = 3,

    Error = 4,

    Fatal = 5,
}
