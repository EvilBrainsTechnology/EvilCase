using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// Which cases the list shows. Not the same thing as <see cref="CaseStatus"/>: this is the control the
/// user sees, and its first two members are the ones no single status can express.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CaseStatusFilter>))]
public enum CaseStatusFilter
{
    /// <summary>
    /// The default (#100): everything that is not closed. The page is for what is on the desk, and an
    /// archive grows without limit.
    /// </summary>
    Open = 0,

    All = 1,

    Active = 2,

    WaitingOnAuthority = 3,

    Closed = 4,
}
