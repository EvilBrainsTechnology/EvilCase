namespace EvilBrains.EvilCase.App.Files;

internal static class FileUrl
{
    /// <summary>
    /// The one address the file content is served from.
    /// </summary>
    public static string Content(Guid fileId)
    {
        return $"/api/files/{fileId}/content";
    }
}
