using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Business.Files.Text;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class FileTools
{
    [McpServerTool(Name = "list_case_files", ReadOnly = true, OpenWorld = false)]
    [Description("Lists the files of a case in upload order, each with its current version and the signature file that signs it.")]
    public static async Task<FileListResponse> ListCaseFiles(IFileReader files, [Description("The case.")] Guid caseId, CancellationToken token)
    {
        var items = await files.ListCaseFiles(caseId, token) ?? throw McpProblem.Of(CaseProblems.NotFound);

        return new FileListResponse { Items = items };
    }

    [McpServerTool(Name = "list_act_files", ReadOnly = true, OpenWorld = false)]
    [Description("Lists the files of an act in upload order, each with its current version and the signature file that signs it.")]
    public static async Task<FileListResponse> ListActFiles(IFileReader files, [Description("The act.")] Guid actId, CancellationToken token)
    {
        var items = await files.ListActFiles(actId, token) ?? throw McpProblem.Of(ActProblems.ActNotFound);

        return new FileListResponse { Items = items };
    }

    [McpServerTool(Name = "list_file_versions", ReadOnly = true, OpenWorld = false)]
    [Description("Lists the versions of a file, newest first, with size, upload time and uploader.")]
    public static async Task<FileVersionListResponse> ListFileVersions(IFileReader files, [Description("The file.")] Guid fileId, CancellationToken token)
    {
        var items = await files.ListFileVersions(fileId, token) ?? throw McpProblem.Of(FileProblems.NotFound);

        return new FileVersionListResponse { Items = items };
    }

    [McpServerTool(Name = "read_file_text", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Reads the text of a file version: a text format (txt, csv, md, json, xml, html, yaml, log) as stored, the text layer of "
            + "a PDF, the paragraphs of a DOCX. The text comes in pages of 50,000 characters with the page count and total length; "
            + "read the next page until the last. A scan or an image has no text and says so. A file over 30 MB is not read. "
            + "Data messages (ZFO), e-mails and p7m content are not read.")]
    public static async Task<FileTextPage> ReadFileText(
        IFileTextReader reader,
        [Description("The file.")] Guid fileId,
        [Description("The version; the current one where omitted.")] int? version,
        [Description("The page, from 1.")] int page = 1,
        CancellationToken token = default)
    {
        var result = await reader.ReadFileText(fileId, version, page, token);

        return result.Outcome switch
        {
            FileTextOutcome.Read => result.Page!,
            FileTextOutcome.FileNotFound => throw McpProblem.Of(FileProblems.NotFound),
            FileTextOutcome.UnsupportedFormat => throw McpProblem.Of("No text", "Text is read from text formats, PDF and DOCX only."),
            FileTextOutcome.TooLarge => throw McpProblem.Of("File too large", "The text of a file over 30 MB is not read."),
            FileTextOutcome.NoText => throw McpProblem.Of("No text", "The file carries no text layer; it is a scan or an image."),
            FileTextOutcome.Unreadable => throw McpProblem.Of("Unreadable file", "The content does not parse as its extension says."),
            FileTextOutcome.PageNotFound => throw McpProblem.Of("Page not found", string.Create(CultureInfo.InvariantCulture, $"The text has {result.PageCount} page(s).")),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "rename_case_file", Destructive = false, OpenWorld = false)]
    [Description(
        "Renames a file of a case. Only the last path segment of fileName is kept; it must not be empty, must not exceed 256 "
            + "characters and must be unique among the files of the case.")]
    public static async Task<string> RenameCaseFile(
        IFileWriter writer,
        [Description("The case.")] Guid caseId,
        [Description("The file.")] Guid fileId,
        [Description("The new name.")] FileRenameRequest request,
        CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.RenameCaseFile(caseId, fileId, ValidFileName(request.FileName), token);

        return Answer(outcome, "Another file of the case already carries the name.");
    }

    [McpServerTool(Name = "rename_act_file", Destructive = false, OpenWorld = false)]
    [Description(
        "Renames a file of an act. Only the last path segment of fileName is kept; it must not be empty, must not exceed 256 "
            + "characters and must be unique among the files of the act.")]
    public static async Task<string> RenameActFile(
        IFileWriter writer,
        [Description("The act.")] Guid actId,
        [Description("The file.")] Guid fileId,
        [Description("The new name.")] FileRenameRequest request,
        CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.RenameActFile(actId, fileId, ValidFileName(request.FileName), token);

        return Answer(outcome, "Another file of the act already carries the name.");
    }

    [McpServerTool(Name = "delete_case_file", Destructive = true, OpenWorld = false)]
    [Description("Deletes a file of a case with every version. Cannot be undone.")]
    public static async Task<string> DeleteCaseFile(IFileWriter writer, [Description("The case.")] Guid caseId, [Description("The file.")] Guid fileId, CancellationToken token)
    {
        return Answer(await writer.DeleteCaseFile(caseId, fileId, token));
    }

    [McpServerTool(Name = "delete_act_file", Destructive = true, OpenWorld = false)]
    [Description("Deletes a file of an act with every version. Cannot be undone.")]
    public static async Task<string> DeleteActFile(IFileWriter writer, [Description("The act.")] Guid actId, [Description("The file.")] Guid fileId, CancellationToken token)
    {
        return Answer(await writer.DeleteActFile(actId, fileId, token));
    }

    [McpServerTool(Name = "delete_file_version", Destructive = true, OpenWorld = false)]
    [Description("Deletes one version of a file. The only version is deleted only with the file (delete_case_file, delete_act_file). Cannot be undone.")]
    public static async Task<string> DeleteFileVersion(
        IFileWriter writer, [Description("The file.")] Guid fileId, [Description("The version number.")] int number, CancellationToken token)
    {
        var outcome = await writer.DeleteFileVersion(fileId, number, token);

        return outcome switch
        {
            FileVersionDeleteOutcome.Deleted => "The version was deleted.",
            FileVersionDeleteOutcome.NotFound => throw McpProblem.Of(FileProblems.VersionNotFound),
            FileVersionDeleteOutcome.LastVersion => throw McpProblem.Of(FileProblems.LastVersion, "The only version of a file is deleted only with the file."),
            _ => throw new UnreachableException(),
        };
    }

    private static string ValidFileName(string fileName)
    {
        var lastSegment = FileNameRules.LastSegment(fileName);

        if (FileNameRules.Violation(lastSegment) is { } violation)
            throw McpProblem.Of("One or more validation errors occurred.", $"fileName: {violation}");

        return lastSegment;
    }

    private static string Answer(FileRenameOutcome outcome, string nameTaken)
    {
        return outcome switch
        {
            FileRenameOutcome.Renamed => "The file was renamed.",
            FileRenameOutcome.NotFound => throw McpProblem.Of(FileProblems.NotFound),
            FileRenameOutcome.NameTaken => throw McpProblem.Of(FileProblems.NameTaken, nameTaken),
            _ => throw new UnreachableException(),
        };
    }

    private static string Answer(DeleteOutcome outcome)
    {
        return outcome switch
        {
            DeleteOutcome.Deleted => "The file was deleted.",
            DeleteOutcome.NotFound => throw McpProblem.Of(FileProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }
}
