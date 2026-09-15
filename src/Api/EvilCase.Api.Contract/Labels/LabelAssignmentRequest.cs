namespace EvilBrains.EvilCase.Api.Contract.Labels;

/// <summary>
/// The whole set the owner carries afterwards; a label left out is taken off.
/// </summary>
public sealed record LabelAssignmentRequest
{
    public IReadOnlyList<Guid> LabelIds { get; init; } = [];
}
