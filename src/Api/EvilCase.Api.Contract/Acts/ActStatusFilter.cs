using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ActStatusFilter>))]
public enum ActStatusFilter
{
    All = 0,

    /// <summary>
    /// The acts without a status.
    /// </summary>
    None = 1,

    InProgress = 2,

    Sent = 3,
}
