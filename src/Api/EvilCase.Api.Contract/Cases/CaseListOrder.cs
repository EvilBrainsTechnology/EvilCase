using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseListOrder>))]
public enum CaseListOrder
{
    Date = 0,

    Changed = 1,
}
