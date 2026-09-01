using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Cases;

/// <summary>
/// The closed set from <c>docs/product/vision.md</c>. Serialized by name rather than by number, so the
/// wire format survives a reordering and the stored column stays readable.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseStatus>))]
public enum CaseStatus
{
    Active = 0,

    /// <summary>
    /// The case is with an authority and nothing can move until it answers.
    /// </summary>
    WaitingOnAuthority = 1,

    Closed = 2,
}
