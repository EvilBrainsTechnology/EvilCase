using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ListSortDirection>))]
public enum ListSortDirection
{
    Descending = 0,

    Ascending = 1,
}
