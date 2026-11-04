using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Acts;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ActStatus>))]
public enum ActStatus
{
    InProgress = 0,

    Sent = 1,
}
