using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ContactSortKey>))]
public enum ContactSortKey
{
    Name = 0,

    Changed = 1,
}
