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
    /// The one act of the case with its contact and the contact of its case, or null where the tenant has no
    /// such act.
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
                Contact = act.Contact == null
                    ? null
                    : new ContactListItem
                    {
                        ContactId = act.Contact.Id,
                        Kind = act.Contact.Kind,
                        Name = act.Contact.Name,
                        DataBoxId = act.Contact.DataBoxId,
                        Address = act.Contact.Address,
                    },
                CaseContact = act.Case!.Contact == null
                    ? null
                    : new ContactListItem
                    {
                        ContactId = act.Case.Contact.Id,
                        Kind = act.Case.Contact.Kind,
                        Name = act.Case.Contact.Name,
                        DataBoxId = act.Case.Contact.DataBoxId,
                        Address = act.Case.Contact.Address,
                    },
            })
            .SingleOrDefaultAsync(token);
    }
}
