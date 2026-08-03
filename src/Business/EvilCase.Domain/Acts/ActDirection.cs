using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Acts;

/// <summary>
/// Which way an act travelled, from the case owner's point of view. Serialized by name rather than by
/// number, so the wire format survives a reordering and the stored column stays readable.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ActDirection>))]
public enum ActDirection
{
    /// <summary>
    /// Arrived at the owner — a decision, a notice, a call.
    /// </summary>
    Incoming = 0,

    /// <summary>
    /// Filed by the owner — a submission, an appeal, a statement.
    /// </summary>
    Outgoing = 1,
}
