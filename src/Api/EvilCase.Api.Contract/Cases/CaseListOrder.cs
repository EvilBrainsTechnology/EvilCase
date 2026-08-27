using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(JsonStringEnumConverter<CaseListOrder>))]
public enum CaseListOrder
{
    /// <summary>
    /// The case's own date, newest first (SDD-009).
    /// </summary>
    Date = 0,

    /// <summary>
    /// When the case itself last changed, newest first (SDD-015).
    /// </summary>
    Changed = 1,
}
