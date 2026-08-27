using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Contacts;

/// <summary>
/// How a contact is named by an act. Serialized by name rather than by number, so the wire format
/// survives a reordering.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<ContactActRole>))]
public enum ContactActRole
{
    IssuedBy = 0,

    AddressedTo = 1,

    NumberIssuer = 2,
}
