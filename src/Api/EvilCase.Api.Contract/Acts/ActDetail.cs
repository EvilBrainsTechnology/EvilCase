using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActDetail
{
    public required Guid ActId { get; init; }

    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string ActNumber { get; init; }

    public string? ExternalActNumber { get; init; }

    public ActDirection? Direction { get; init; }

    public required DateOnly Date { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public ContactListItem? Contact { get; init; }

    /// <summary>
    /// Carried so a screen can tell the act's contact from the case's.
    /// </summary>
    public ContactListItem? CaseContact { get; init; }
}
