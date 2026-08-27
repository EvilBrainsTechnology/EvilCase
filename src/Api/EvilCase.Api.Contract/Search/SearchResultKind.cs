using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Search;

[JsonConverter(typeof(JsonStringEnumConverter<SearchResultKind>))]
public enum SearchResultKind
{
    Case = 0,

    Act = 1,
}
