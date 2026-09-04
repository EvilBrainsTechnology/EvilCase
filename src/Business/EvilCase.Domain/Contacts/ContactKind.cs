using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Contacts;

/// <summary>
/// Flat: an official carries no link to the authority it acts for.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<ContactKind>))]
public enum ContactKind
{
    Authority = 0,

    Official = 1,

    Person = 2,
}
