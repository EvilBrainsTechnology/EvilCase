using System.ComponentModel;
using System.Diagnostics;
using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Business.Files.Links;
using EvilBrains.EvilCase.Domain.Files;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

/// <summary>
/// File bytes never travel through MCP: a tool hands out a one-time link, which plain HTTP spends.
/// </summary>
[McpServerToolType]
internal static class FileLinkTools
{
    private const string LocalPath = "<local path>";

    [McpServerTool(Name = "create_file_upload_link", Destructive = false, OpenWorld = false)]
    [Description(
        "Creates a one-time link that uploads a file: a new file onto a case (caseId) or an act (actId), or a new version of a "
            + "file (fileId); name exactly one. fileName is the name the upload arrives under (at most 256 characters); a new "
            + "version keeps the extension of its file. Send the raw bytes as an HTTP PUT to the URL, as the curl example does; "
            + "at most 100 MB. The link is valid 10 minutes and spent by its first use. The upload answers with the file; 200 "
            + "where the content equals the current version and nothing was stored.")]
    public static async Task<FileLink> CreateFileUploadLink(
        IFileLinkWriter writer,
        IHttpContextAccessor httpContextAccessor,
        [Description("The name the file arrives under.")] string fileName,
        [Description("The case a new file goes onto.")] Guid? caseId = null,
        [Description("The act a new file goes onto.")] Guid? actId = null,
        [Description("The file a new version goes onto.")] Guid? fileId = null,
        CancellationToken token = default)
    {
        var (kind, targetId) = (caseId, actId, fileId) switch
        {
            ({ } id, null, null) => (FileLinkKind.CaseUpload, id),
            (null, { } id, null) => (FileLinkKind.ActUpload, id),
            (null, null, { } id) => (FileLinkKind.VersionUpload, id),
            _ => throw McpProblem.Of("One or more validation errors occurred.", "Name exactly one of caseId, actId and fileId."),
        };

        var lastSegment = FileNameRules.LastSegment(fileName);

        if (FileNameRules.Violation(lastSegment) is { } violation)
            throw McpProblem.Of("One or more validation errors occurred.", $"fileName: {violation}");

        var (link, expires) = await CreateFileLink(writer, httpContextAccessor.HttpContext!, kind, targetId, version: null, token);
        var url = $"{link}&{FileLinkRoute.FileNameParameter}={Uri.EscapeDataString(lastSegment)}";

        return new FileLink
        {
            Method = HttpMethods.Put,
            Url = url,
            Expires = expires,
            Curl = $"curl --fail --upload-file '{LocalPath}' '{url}'",
        };
    }

    [McpServerTool(Name = "create_file_download_link", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Creates a one-time link that downloads the content of a file version with an HTTP GET, as the curl example does. The "
            + "link is valid 10 minutes and spent by its first use. read_file_text reads the text of a file without a download.")]
    public static async Task<FileLink> CreateFileDownloadLink(
        IFileLinkWriter writer,
        IHttpContextAccessor httpContextAccessor,
        [Description("The file.")] Guid fileId,
        [Description("The version; the current one where omitted.")] int? version = null,
        CancellationToken token = default)
    {
        var (url, expires) = await CreateFileLink(writer, httpContextAccessor.HttpContext!, FileLinkKind.Download, fileId, version, token);

        return new FileLink
        {
            Method = HttpMethods.Get,
            Url = url,
            Expires = expires,
            Curl = $"curl --fail --output '{LocalPath}' '{url}'",
        };
    }

    /// <summary>
    /// The link is absolute, from the scheme and host the MCP request arrived at; the host filter and the forwarded headers
    /// vouch for both. The token that asks owns the link, so its revocation takes the link along.
    /// </summary>
    private static async Task<(string Url, DateTime Expires)> CreateFileLink(
        IFileLinkWriter writer, HttpContext httpContext, FileLinkKind kind, Guid targetId, int? version, CancellationToken token)
    {
        var apiTokenId = Guid.Parse(httpContext.User.FindFirstValue(AuthClaims.ApiTokenId)!, CultureInfo.InvariantCulture);

        var result = await writer.CreateFileLink(kind, targetId, version, apiTokenId, token);

        if (result.Outcome == FileLinkCreateOutcome.TargetNotFound)
        {
            throw kind switch
            {
                FileLinkKind.CaseUpload => McpProblem.Of(CaseProblems.NotFound),
                FileLinkKind.ActUpload => McpProblem.Of(ActProblems.ActNotFound),
                FileLinkKind.VersionUpload => McpProblem.Of(FileProblems.NotFound),
                FileLinkKind.Download => McpProblem.Of(version is null ? FileProblems.NotFound : FileProblems.VersionNotFound),
                _ => throw new UnreachableException(),
            };
        }

        var request = httpContext.Request;
        var url = $"{request.Scheme}://{request.Host}{request.PathBase}{FileLinkRoute.Path}?{FileLinkRoute.SecretParameter}={result.Secret}";

        return (url, result.Expires!.Value);
    }
}
