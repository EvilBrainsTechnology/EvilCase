using System.Globalization;

namespace EvilBrains.EvilCase.App.Files;

internal static class FileUrl
{
    /// <summary>
    /// The one address the file content is served from; a version number names an older version.
    /// </summary>
    public static string Content(Guid fileId, int? version)
    {
        return version is null
            ? $"/api/files/{fileId}/content"
            : $"/api/files/{fileId}/content?version={version.Value.ToString(CultureInfo.InvariantCulture)}";
    }
}
