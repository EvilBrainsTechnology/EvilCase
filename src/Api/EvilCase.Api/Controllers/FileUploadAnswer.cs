using EvilBrains.EvilCase.Api.Contract.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

/// <summary>
/// The refusals a multipart upload and an upload through a file link share.
/// </summary>
internal static class FileUploadAnswer
{
    public static ActionResult FileTooLargeProblem(this ControllerBase controller)
    {
        return controller.Problem(detail: "The file exceeds the 100 MB upload limit.", statusCode: StatusCodes.Status413PayloadTooLarge, title: "File too large");
    }

    public static ActionResult ExtensionMismatchProblem(this ControllerBase controller)
    {
        controller.ModelState.AddModelError("fileName", "The new version must keep the extension of the file.");

        return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
    }

    public static ActionResult? InvalidUploadMetadataProblem(this ControllerBase controller, string fileName, string? mediaType)
    {
        if (FileNameRules.Violation(fileName) is { } violation)
        {
            controller.ModelState.AddModelError("fileName", violation);
            return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        if (mediaType is { Length: > FileLimits.MaxMediaTypeLength })
        {
            controller.ModelState.AddModelError("mediaType", $"The media type must not exceed {FileLimits.MaxMediaTypeLength} characters.");
            return controller.ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        return null;
    }
}
