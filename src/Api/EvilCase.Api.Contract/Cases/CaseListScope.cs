using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

[JsonConverter(typeof(StrictJsonStringEnumConverter<CaseListScope>))]
public enum CaseListScope
{
    /// <summary>
    /// Only the cases without a parent.
    /// </summary>
    RootOnly = 0,

    All = 1,
}
