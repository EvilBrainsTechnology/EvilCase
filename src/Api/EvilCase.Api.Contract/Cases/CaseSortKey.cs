using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseSortKey>))]
public enum CaseSortKey
{
    Date = 0,

    Changed = 1,

    Title = 2,

    CaseNumber = 3,
}
