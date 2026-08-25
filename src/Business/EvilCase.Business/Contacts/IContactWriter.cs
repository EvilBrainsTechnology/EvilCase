using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Creates, edits and deletes a contact.
/// </summary>
public interface IContactWriter
{
    public Task<ContactListItem> CreateContact(ContactEditRequest request, CancellationToken token);

    public Task<ContactUpdateOutcome> UpdateContact(Guid contactId, ContactEditRequest request, CancellationToken token);

    public Task<ContactDeleteOutcome> DeleteContact(Guid contactId, CancellationToken token);
}
