using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Contacts;

/// <summary>
/// The three kinds from <c>docs/product/vision.md</c>. Flat: an official carries no link to the
/// authority it acts for. Serialized by name rather than by number, so the wire format survives a
/// reordering and the stored column stays readable.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContactKind>))]
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
