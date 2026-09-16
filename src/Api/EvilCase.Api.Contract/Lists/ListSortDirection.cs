using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Lists;

[JsonConverter(typeof(StrictJsonStringEnumConverter<ListSortDirection>))]
public enum ListSortDirection
{
    Ascending = 0,

    Descending = 1,
}
