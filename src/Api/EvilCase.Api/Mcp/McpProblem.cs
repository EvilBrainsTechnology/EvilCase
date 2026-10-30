using System.ComponentModel.DataAnnotations;
using ModelContextProtocol;

namespace EvilBrains.EvilCase.Api.Mcp;

/// <summary>
/// A tool answers a refusal as a tool error carrying the Problem Details title and detail its REST endpoint answers with.
/// </summary>
internal static class McpProblem
{
    public static McpException Of(string title, string? detail = null)
    {
        return new McpException(detail is null ? title : $"{title}: {detail}");
    }

    /// <summary>
    /// The data annotations REST model validation checks on the same DTO.
    /// </summary>
    public static void Validate(object request)
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
            return;

        var errors = results.Select(static result => $"{string.Join(", ", result.MemberNames)}: {result.ErrorMessage}");

        throw Of("One or more validation errors occurred.", string.Join(' ', errors));
    }
}
