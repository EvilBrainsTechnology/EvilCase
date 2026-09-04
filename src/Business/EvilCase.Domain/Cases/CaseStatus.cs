using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Cases;

[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseStatus>))]
public enum CaseStatus
{
    Active = 0,

    WaitingOnAuthority = 1,

    Closed = 2,
}
