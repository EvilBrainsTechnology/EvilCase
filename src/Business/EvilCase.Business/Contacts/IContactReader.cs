using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

public interface IContactReader
{
    public Task<IReadOnlyList<ContactListItem>> ListContacts(ContactListRequest request, CancellationToken token);

    public Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token);
}
