using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal static class CaseDetailQuery
{
    public static async Task<CaseDetail?> DetailOf(ApplicationDbContext context, Guid caseId, CancellationToken token)
    {
        return await context.Cases
            .WithId(caseId)
            .Select(@case => new CaseDetail
            {
                CaseId = @case.Id,
                CaseNumber = @case.CaseNumber,
                ExternalCaseNumber = @case.ExternalCaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Description = @case.Description,
                Status = @case.Status,
                Contact = @case.Contact == null
                    ? null
                    : new ContactListItem
                    {
                        ContactId = @case.Contact.Id,
                        Kind = @case.Contact.Kind,
                        Name = @case.Contact.Name,
                        DataBoxId = @case.Contact.DataBoxId,
                        Address = @case.Contact.Address,
                    },
                ParentCase = @case.ParentCase == null
                    ? null
                    : new CaseListItem
                    {
                        CaseId = @case.ParentCase.Id,
                        CaseNumber = @case.ParentCase.CaseNumber,
                        ExternalCaseNumber = @case.ParentCase.ExternalCaseNumber,
                        Title = @case.ParentCase.Title,
                        Date = @case.ParentCase.Date,
                        Status = @case.ParentCase.Status,
                        Changed = @case.ParentCase.Updated ?? @case.ParentCase.Created,
                    },
                Ancestors = context.CaseAncestors(@case.Id)
                    .OrderByDescending(static ancestor => ancestor.Depth)
                    .Select(static ancestor => new CaseCrumb { CaseId = ancestor.CaseId, Title = ancestor.Title })
                    .ToList(),
                Labels = @case.Labels
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
}
