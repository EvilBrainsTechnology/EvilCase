using EvilBrains.EvilCase.Domain.Files;

namespace EvilBrains.EvilCase.Business.Files.Links;

/// <summary>
/// What a used link names: the request that presented it runs as this user in this tenant.
/// </summary>
public sealed record ConsumedFileLink
{
    public required Guid TenantId { get; init; }

    public required Guid UserId { get; init; }

    public required Guid ApiTokenId { get; init; }

    public required FileLinkKind Kind { get; init; }

    public required Guid TargetId { get; init; }

    public int? Version { get; init; }
}
