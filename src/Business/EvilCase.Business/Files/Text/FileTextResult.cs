using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Text;

public sealed record FileTextResult
{
    public required FileTextOutcome Outcome { get; init; }

    /// <summary>
    /// Set where the outcome is <see cref="FileTextOutcome.Read"/>.
    /// </summary>
    public FileTextPage? Page { get; init; }

    /// <summary>
    /// Set where the outcome is <see cref="FileTextOutcome.PageNotFound"/>.
    /// </summary>
    public int? PageCount { get; init; }
}
