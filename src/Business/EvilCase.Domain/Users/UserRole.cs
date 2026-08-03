using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Users;

/// <summary>
/// The user entity, the role claim and the client all name these values. Serialized by name rather than
/// by number, so the wire format survives a reordering and matches what the claim carries.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
public enum UserRole
{
    User = 0,

    Admin = 1,
}
