using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.Logs;

public record ClientLogEntry
{
    public const int MessageMaxLength = 4000;

    public const int ExceptionMaxLength = 8000;

    public const int CategoryMaxLength = 200;

    public const int UrlMaxLength = 2000;

    public required DateTimeOffset Timestamp { get; init; }

    public required ClientLogLevel Level { get; init; }

    [StringLength(MessageMaxLength)]
    public required string Message { get; init; }

    [StringLength(CategoryMaxLength)]
    public string? Category { get; init; }

    [StringLength(ExceptionMaxLength)]
    public string? Exception { get; init; }

    [StringLength(UrlMaxLength)]
    public string? Url { get; init; }
}
