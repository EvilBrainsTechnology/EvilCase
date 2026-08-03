using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(JsonStringEnumConverter<CaseStatusFilter>))]
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
