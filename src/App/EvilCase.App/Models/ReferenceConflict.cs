using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.App.Models;

internal static class ReferenceConflict
{
    /// <summary>
    /// Whether a 409 says that a contact or a label the request names no longer exists.
    /// </summary>
    public static bool NamesUnknownContactOrLabel(ApiException exception)
    {
        return exception.ResponseBody is { } body
            && (body.Contains(ContactProblems.UnknownContact, StringComparison.Ordinal)
                || body.Contains(LabelProblems.UnknownLabel, StringComparison.Ordinal));
    }
}
