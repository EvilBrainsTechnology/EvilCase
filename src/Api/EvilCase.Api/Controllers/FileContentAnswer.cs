using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace EvilBrains.EvilCase.Api.Controllers;

internal static class FileContentAnswer
{
    /// <summary>
    /// Only the types a browser shows natively are inline, under the type the extension gives; any other stored document, PDF
    /// included, is an opaque attachment (SDD-012). nosniff comes from SecurityHeadersMiddleware.
    /// </summary>
    public static ActionResult FileContent(this ControllerBase controller, FileDownload download)
    {
        if (InlineFileTypes.MediaType(download.FileName) is not { } inlineMediaType)
            return controller.File(download.Content, "application/octet-stream", download.FileName, enableRangeProcessing: true);

        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(download.FileName);
        controller.Response.Headers.ContentDisposition = disposition.ToString();

        return controller.File(download.Content, inlineMediaType, enableRangeProcessing: true);
    }
}
