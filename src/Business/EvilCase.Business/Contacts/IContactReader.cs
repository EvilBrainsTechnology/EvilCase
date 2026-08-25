using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Reads contacts for the screens that show them.
/// </summary>
public interface IContactReader
{
    public Task<IReadOnlyList<ContactListItem>> ListContacts(ContactListRequest request, CancellationToken token);

    public Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token);

    public Task<ContactListItem> GetDefaultContact(CancellationToken token);
}
