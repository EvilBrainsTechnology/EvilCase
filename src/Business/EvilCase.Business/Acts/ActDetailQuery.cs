using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Reads the header of one act.
/// </summary>
internal static class ActDetailQuery
{
    /// <summary>
    /// The one act of the case with both its contacts, or null where the tenant has no such act.
    /// </summary>
    public static async Task<ActDetail?> DetailOf(this IQueryable<Act> acts, Guid caseId, Guid actId, CancellationToken token)
    {
        return await acts
            .OfCase(caseId)
            .WithId(actId)
            .Select(static act => new ActDetail
            {
                ActId = act.Id,
                CaseId = act.CaseId,
                CaseNumber = act.Case!.CaseNumber,
                ActNumber = act.ActNumber,
                ExternalActNumber = act.ExternalActNumber,
                Direction = act.Direction,
                Date = act.Date,
                Title = act.Title,
                Description = act.Description,
                IssuedByContact = new ContactListItem
                {
                    ContactId = act.IssuedByContact!.Id,
                    Kind = act.IssuedByContact.Kind,
                    Name = act.IssuedByContact.Name,
                    DataBoxId = act.IssuedByContact.DataBoxId,
                    Address = act.IssuedByContact.Address,
                },
                AddressedToContact = act.AddressedToContact == null
                    ? null
                    : new ContactListItem
                    {
                        ContactId = act.AddressedToContact.Id,
                        Kind = act.AddressedToContact.Kind,
                        Name = act.AddressedToContact.Name,
                        DataBoxId = act.AddressedToContact.DataBoxId,
                        Address = act.AddressedToContact.Address,
                    },
            })
            .SingleOrDefaultAsync(token);
    }
}
