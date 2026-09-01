using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseStatusFilter>))]
public enum CaseStatusFilter
{
    /// <summary>
    /// Everything that is not closed.
    /// </summary>
    Open = 0,

    All = 1,

    Active = 2,

    WaitingOnAuthority = 3,

    Closed = 4,
}
