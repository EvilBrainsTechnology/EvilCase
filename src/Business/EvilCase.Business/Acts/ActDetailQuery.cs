using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal static class ActDetailQuery
{
    public static async Task<ActDetail?> ActDetailOf(this ApplicationDbContext context, Guid actId, CancellationToken token)
    {
        return await context.Acts
            .WithId(actId)
            .Select(act => new ActDetail
            {
                ActId = act.Id,
                CaseId = act.CaseId,
                CaseNumber = act.Case!.CaseNumber,
                CaseTitle = act.Case.Title,
                CaseAncestors = context.CaseAncestors(act.CaseId)
                    .Join(context.Cases, static ancestor => ancestor.CaseId, static parent => parent.Id, static (ancestor, parent) => new { ancestor.Depth, parent.Id, parent.Title })
                    .OrderByDescending(static step => step.Depth)
                    .Select(static step => new CaseCrumb { CaseId = step.Id, Title = step.Title })
                    .ToList(),
                CaseStatus = act.Case.Status,
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
                Labels = act.Labels
                    .OrderBy(static assignment => EF.Functions.Collate(assignment.Label!.Name, DatabaseFunctions.CzechCollation))
                    .Select(static assignment => new LabelItem
                    {
                        LabelId = assignment.LabelId,
                        Name = assignment.Label!.Name,
                        Color = assignment.Label!.Color,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync(token);
    }

    public static IQueryable<ActTimelineItem> AsTimelineItems(this IQueryable<Act> acts, Guid currentActId)
    {
        return acts.Select(act => new ActTimelineItem
        {
            ActId = act.Id,
            Title = act.Title,
            Date = act.Date,
            Direction = act.Direction,
            IsCurrent = act.Id == currentActId,
        });
    }
}
