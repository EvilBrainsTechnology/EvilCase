using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Contacts;

/// <summary>
/// The three kinds from <c>docs/product/vision.md</c>. Flat: an official carries no link to the
/// authority it acts for. Serialized by name rather than by number, so the wire format survives a
/// reordering and the stored column stays readable.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<ContactKind>))]
public enum ContactKind
{
    /// <summary>
    /// A court, an office, an institution.
    /// </summary>
    Authority = 0,

    /// <summary>
    /// A named human acting for an authority. Which authority is not recorded — the name is what says
    /// where they work.
    /// </summary>
    Official = 1,

    Person = 2,
}
