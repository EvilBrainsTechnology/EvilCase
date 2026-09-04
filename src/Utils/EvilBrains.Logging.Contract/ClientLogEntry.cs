using System.ComponentModel.DataAnnotations;

namespace EvilBrains.Logging.Contract;

public record ClientLogEntry
{
    public const int MessageTemplateMaxLength = 4000;

    public const int ExceptionMaxLength = 8000;

    public const int CategoryMaxLength = 200;

    public const int UrlMaxLength = 2000;

    public const int MaxProperties = 16;

    public const int PropertyValueMaxLength = 512;

    public const int IdentifierMaxLength = 64;

    public required DateTimeOffset Timestamp { get; init; }

    public required ClientLogLevel Level { get; init; }

    [StringLength(MessageTemplateMaxLength)]
    public required string MessageTemplate { get; init; }

    [MaxLength(MaxProperties)]
    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    /// <summary>
    /// Absent, the entry inherits the identifiers of the upload that carried it.
    /// </summary>
    [StringLength(IdentifierMaxLength)]
    public string? RequestId { get; init; }

    [StringLength(IdentifierMaxLength)]
    public string? CorrelationId { get; init; }

    [StringLength(CategoryMaxLength)]
    public string? Category { get; init; }

    [StringLength(ExceptionMaxLength)]
    public string? Exception { get; init; }

    [StringLength(UrlMaxLength)]
    public string? Url { get; init; }
}
