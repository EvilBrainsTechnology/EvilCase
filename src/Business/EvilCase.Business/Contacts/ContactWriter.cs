using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactWriter(IDbSession session) : IContactWriter
{
    public async Task<ContactUpdateOutcome> Update(Guid id, ContactEditRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var context = session.Current;

        var contact = await context.Contacts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contact is null)
            return ContactUpdateOutcome.NotFound;

        contact.Name = request.Name.Trim();
        contact.Kind = request.Kind;
        contact.DataBoxId = Trimmed(request.DataBoxId);
        contact.Address = Trimmed(request.Address);

        await context.SaveChangesAsync(cancellationToken);

        return ContactUpdateOutcome.Updated;
    }

    public async Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var context = session.Current;

        var contact = await context.Contacts.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (contact is null)
            return ContactDeleteOutcome.NotFound;

        if (await context.Users.AnyAsync(user => user.DefaultContactId == id, cancellationToken))
            return ContactDeleteOutcome.DefaultContact;

        if (await ReferencesTo(context, id).AnyAsync(cancellationToken))
            return ContactDeleteOutcome.Referenced;

        context.Contacts.Remove(contact);
        await context.SaveChangesAsync(cancellationToken);

        return ContactDeleteOutcome.Deleted;
    }

    /// <summary>
    /// Every row that names the contact, as one query. Internal so a test reads the SQL the delete really runs.
    /// </summary>
    internal static IQueryable<Guid> ReferencesTo(ApplicationDbContext context, Guid contactId) =>
        context.ExternalCaseNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id)
            .Concat(context.Acts.Where(act => act.IssuedByContactId == contactId).Select(act => act.Id))
            .Concat(context.Acts.Where(act => act.AddressedToContactId == contactId).Select(act => act.Id))
            .Concat(context.ExternalActNumbers.Where(number => number.AssignedByContactId == contactId).Select(number => number.Id));

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
