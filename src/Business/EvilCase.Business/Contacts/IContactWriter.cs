using EvilBrains.EvilCase.Api.Contract.Contacts;

namespace EvilBrains.EvilCase.Business.Contacts;

/// <summary>
/// Edits and deletes a contact.
/// </summary>
public interface IContactWriter
{
    public Task<ContactUpdateOutcome> Update(Guid id, ContactEditRequest request, CancellationToken cancellationToken);

    public Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken);
}
