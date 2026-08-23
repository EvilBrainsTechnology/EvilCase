using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Reads contacts for the screens that show them.
/// </summary>
public interface IContactReader
{
    public Task<IReadOnlyList<ContactListItem>> List(ContactListRequest request, CancellationToken cancellationToken);

    public Task<ContactDetail?> Detail(Guid id, CancellationToken cancellationToken);
}
