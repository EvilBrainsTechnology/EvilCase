using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Acts;

/// <summary>
/// From the case owner's point of view.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<ActDirection>))]
public enum ActDirection
{
    Incoming = 0,

    Outgoing = 1,
}
