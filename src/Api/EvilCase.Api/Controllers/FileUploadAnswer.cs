using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// The answers the file uploads, through the API and through a file link, the version restore and the file renames share.
/// </summary>
internal static class FileUploadAnswer
{
    private const string FileTransferController = "FileTransfer";

    public static ActionResult<FileListItem> Answer(this ControllerBase controller, UploadFileResult result, string ownerNotFound)
    {
        return result.Outcome switch
        {
            UploadFileOutcome.Uploaded or UploadFileOutcome.VersionAdded => controller.CreatedAtAction(
                nameof(Controllers.FileTransferController.DownloadFileContent), FileTransferController, new { fileId = result.File!.FileId }, result.File),
            UploadFileOutcome.Unchanged => controller.Ok(result.File),
            UploadFileOutcome.OwnerNotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: ownerNotFound),
            _ => throw new UnreachableException(),
        };
    }

    public static ActionResult<FileListItem> Answer(this ControllerBase controller, FileVersionUploadResult result)
    {
        return result.Outcome switch
        {
            FileVersionUploadOutcome.VersionAdded => controller.CreatedAtAction(
                nameof(Controllers.FileTransferController.DownloadFileContent),
                FileTransferController,
                new { fileId = result.File!.FileId, version = result.File.Version },
                result.File),
            FileVersionUploadOutcome.Unchanged => controller.Ok(result.File),
            FileVersionUploadOutcome.FileNotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            FileVersionUploadOutcome.ExtensionMismatch => ExtensionMismatchProblem(controller),
            FileVersionUploadOutcome.VersionNotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.VersionNotFound),
            _ => throw new UnreachableException(),
        };
    }

    public static ActionResult RenameAnswer(this ControllerBase controller, FileRenameOutcome outcome, string ownerWord)
    {
        return outcome switch
        {
            FileRenameOutcome.Renamed => controller.NoContent(),
            FileRenameOutcome.NotFound => controller.Problem(statusCode: StatusCodes.Status404NotFound, title: FileProblems.NotFound),
            FileRenameOutcome.NameTaken => controller.Problem(
                detail: FileProblems.NameTakenDetail(ownerWord), statusCode: StatusCodes.Status409Conflict, title: FileProblems.NameTaken),
            _ => throw new UnreachableException(),
        };
    }

    public static ActionResult FileTooLargeProblem(this ControllerBase controller)
    {
        return controller.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");
    }

    public static ActionResult? FileNameProblem(this ControllerBase controller, string fileName)
    {
        if (FileNameRules.Violation(fileName) is not { } violation)
            return null;

        controller.ModelState.AddModelError("fileName", violation);

        return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }

    public static ActionResult? InvalidUploadMetadataProblem(this ControllerBase controller, string fileName, string? mediaType)
    {
        if (controller.FileNameProblem(fileName) is { } problem)
            return problem;

        if (mediaType is { Length: > FileLimits.MaxMediaTypeLength })
        {
            controller.ModelState.AddModelError("mediaType", $"The media type must not exceed {FileLimits.MaxMediaTypeLength} characters.");
            return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private static ActionResult ExtensionMismatchProblem(ControllerBase controller)
    {
        controller.ModelState.AddModelError("fileName", "The new version must keep the extension of the file.");

        return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }
}
