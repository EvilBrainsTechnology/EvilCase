using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Users;

/// <summary>
/// The role claim and the client carry these names; renaming breaks both.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<UserRole>))]
public enum UserRole
{
    User = 0,

    Admin = 1,
}
