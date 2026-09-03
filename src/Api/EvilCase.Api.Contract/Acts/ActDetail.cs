using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActDetail
{
    public required Guid ActId { get; init; }

    /// <summary>
    /// The case the act sits in; the screens build their links to it from here, not from the route.
    /// </summary>
    public required Guid CaseId { get; init; }

    /// <summary>
    /// The number of the case the act sits in, read as the link text.
    /// </summary>
    public required string CaseNumber { get; init; }

    public required string ActNumber { get; init; }

    /// <summary>
    /// The reference number another authority gave this act, null where none is recorded (SDD-010).
    /// </summary>
    public string? ExternalNumber { get; init; }

    public required ActDirection Direction { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The sender, which every act names (SDD-010).
    /// </summary>
    public required ContactListItem IssuedByContact { get; init; }

    /// <summary>
    /// The recipient, null where the act names none (SDD-010).
    /// </summary>
    public ContactListItem? AddressedToContact { get; init; }
}
