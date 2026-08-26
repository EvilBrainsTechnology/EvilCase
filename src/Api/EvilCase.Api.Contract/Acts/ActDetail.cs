using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActDetail
{
    public required Guid Id { get; init; }

    /// <summary>
    /// The number of the case the act sits in, for the link back to it.
    /// </summary>
    public required string CaseNumber { get; init; }

    public required string ActNumber { get; init; }

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
