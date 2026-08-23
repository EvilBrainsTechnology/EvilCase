using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Contacts;

internal sealed class ContactReader(IDbSession dbSession) : IContactReader
{
    public async Task<IReadOnlyList<ContactListItem>> List(ContactListRequest request, CancellationToken cancellationToken)
    {
        return await dbSession.Current.Contacts
            .MatchingSearch(request.Search)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
    }

    // A detail is not a list query: the header, the default-contact flag and the three act sources
    // are separate reads, merged here.
    public async Task<ContactDetail?> Detail(Guid id, CancellationToken cancellationToken)
    {
        var context = dbSession.Current;

        var contact = await context.Contacts.DetailOf(id, cancellationToken);
        if (contact is null)
            return null;

        var isDefault = await context.Users
            .WithDefaultContact(id)
            .AnyAsync(cancellationToken);

        var cases = await context.ExternalCaseNumbers
            .AssignedByContact(id)
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .ToListAsync(cancellationToken);

        var issuedBy = await context.Acts
            .IssuedByContact(id)
            .AsIssuedByOccurrences()
            .ToListAsync(cancellationToken);

        var addressedTo = await context.Acts
            .AddressedToContact(id)
            .AsAddressedToOccurrences()
            .ToListAsync(cancellationToken);

        var numberIssuer = await context.ExternalActNumbers
            .AssignedByContact(id)
            .AsNumberIssuerOccurrences()
            .ToListAsync(cancellationToken);

        return contact with
        {
            IsDefault = isDefault,
            Cases = cases,
            Acts = ContactOccurrences.InDisplayOrder(issuedBy, addressedTo, numberIssuer),
        };
    }
}
