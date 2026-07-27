using System.ComponentModel.DataAnnotations;

namespace EvilBrains.AI.Secrets;

internal sealed record SecretsSettings
{
    [Required]
    public required string Url { get; init; }

    [Required]
    public required string ProjectId { get; init; }

    [Required]
    public required string Environment { get; init; }

    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}
