using System.Text.Json.Serialization;
using EvilBrains.EvilCase.Domain.Json;

namespace EvilBrains.EvilCase.Domain.Files;

/// <summary>
/// What a one-time file link does and what its target id names.
/// </summary>
[JsonConverter(typeof(StrictJsonStringEnumConverter<FileLinkKind>))]
public enum FileLinkKind
{
    /// <summary>
    /// Downloads a file version; the target is the file.
    /// </summary>
    Download = 0,

    /// <summary>
    /// Uploads a new file onto a case.
    /// </summary>
    CaseUpload = 1,

    /// <summary>
    /// Uploads a new file onto an act.
    /// </summary>
    ActUpload = 2,

    /// <summary>
    /// Uploads a new version of a file.
    /// </summary>
    VersionUpload = 3,
}
