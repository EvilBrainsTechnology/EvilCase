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

    /// <summary>
    /// The archive of the case's own files, current versions only.
    /// </summary>
    public static string CaseArchive(Guid caseId)
    {
        return $"/api/cases/{caseId}/files/archive";
    }

    /// <summary>
    /// The archive of the act's files, current versions only.
    /// </summary>
    public static string ActArchive(Guid actId)
    {
        return $"/api/acts/{actId}/files/archive";
    }

    /// <summary>
    /// The same archive with every version of each file.
    /// </summary>
    public static string WithAllVersions(string archiveUrl)
    {
        return archiveUrl + "?includeVersions=true";
    }
}
