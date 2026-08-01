using System.Diagnostics.CodeAnalysis;

namespace EvilBrains.Logging.Contract;

/// <summary>
/// Truncation shared by both halves of the pipeline: the browser truncates before serializing and the
/// server truncates again, so the rule has to be one piece of code — halves that disagree lose whole
/// batches on the side that rejects what the other emitted.
/// </summary>
public static class ClientLogText
{
    /// <summary>
    /// Cutting between a high and a low surrogate leaves a lone surrogate, which no UTF-16 consumer
    /// accepts — the JSON writer among them.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
            return value;

        return value[..(char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength)];
    }
}
