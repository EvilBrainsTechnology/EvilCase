using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ActSortKey>))]
public enum ActSortKey
{
    Date = 0,

    Changed = 1,

    Title = 2,

    ActNumber = 3,
}
