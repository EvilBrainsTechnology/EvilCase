using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Export;

internal static class TenantExportQuery
{
    /// <summary>
    /// The order folders claim their names in: by date, then by filing.
    /// </summary>
    public static IQueryable<OutlinedCase> AsOutlinedCases(this IQueryable<Case> cases)
    {
        return cases
            .OrderBy(static @case => @case.Date)
            .ThenBy(static @case => @case.Created)
            .ThenBy(static @case => @case.Id)
            .Select(static @case => new OutlinedCase
            {
                CaseId = @case.Id,
                ParentCaseId = @case.ParentCaseId,
                Date = @case.Date,
                Title = @case.Title,
            });
    }

    /// <summary>
    /// The order folders claim their names in: by date, then by filing.
    /// </summary>
    public static IQueryable<OutlinedAct> AsOutlinedActs(this IQueryable<Act> acts)
    {
        return acts
            .OrderBy(static act => act.Date)
            .ThenBy(static act => act.Created)
            .ThenBy(static act => act.Id)
            .Select(static act => new OutlinedAct
            {
                ActId = act.Id,
                CaseId = act.CaseId,
                Date = act.Date,
                Title = act.Title,
            });
    }

    /// <summary>
    /// The case's own fields, its comments and files left to the reader.
    /// </summary>
    public static async Task<ExportedCase> ReadExportedCase(this IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        return await cases
            .WithId(caseId)
            .Select(static @case => new ExportedCase
            {
                CaseId = @case.Id,
                CaseNumber = @case.CaseNumber,
                ExternalCaseNumber = @case.ExternalCaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Description = @case.Description,
                Status = @case.Status,
                ParentCaseId = @case.ParentCaseId,
                Contact = @case.Contact == null ? null : new ExportedContactReference { ContactId = @case.Contact.Id, Name = @case.Contact.Name },
                Labels = @case.Labels
                    .OrderBy(static assignment => EF.Functions.Collate(assignment.Label!.Name, DatabaseFunctions.CzechCollation))
                    .Select(static assignment => new ExportedLabelReference { LabelId = assignment.LabelId, Name = assignment.Label!.Name })
                    .ToList(),
                Created = @case.Created,
                Updated = @case.Updated,
            })
            .SingleAsync(token);
    }

    /// <summary>
    /// The act's own fields, its comments and files left to the reader.
    /// </summary>
    public static async Task<ExportedAct> ReadExportedAct(this IQueryable<Act> acts, Guid actId, CancellationToken token)
    {
        return await acts
            .WithId(actId)
            .Select(static act => new ExportedAct
            {
                ActId = act.Id,
                ActNumber = act.ActNumber,
                ExternalActNumber = act.ExternalActNumber,
                Date = act.Date,
                Title = act.Title,
                Description = act.Description,
                Direction = act.Direction,
                CaseId = act.CaseId,
                Contact = act.Contact == null ? null : new ExportedContactReference { ContactId = act.Contact.Id, Name = act.Contact.Name },
                Labels = act.Labels
                    .OrderBy(static assignment => EF.Functions.Collate(assignment.Label!.Name, DatabaseFunctions.CzechCollation))
                    .Select(static assignment => new ExportedLabelReference { LabelId = assignment.LabelId, Name = assignment.Label!.Name })
                    .ToList(),
                Created = act.Created,
                Updated = act.Updated,
            })
            .SingleAsync(token);
    }

    /// <summary>
    /// The join is what carries the e-mail — <see cref="Comment"/> has no <c>User</c> navigation.
    /// </summary>
    public static IQueryable<ExportedComment> AsExportedComments(this IQueryable<Comment> comments, IQueryable<User> users)
    {
        return comments.Join(
            users,
            static comment => comment.UserId,
            static user => user.Id,
            static (comment, user) => new ExportedComment
            {
                CommentId = comment.Id,
                Body = comment.Body,
                AuthorEmail = user.Email,
                Created = comment.Created,
                Updated = comment.Updated,
            });
    }

    public static IQueryable<ExportedComment> InDiaryOrder(this IQueryable<ExportedComment> comments)
    {
        return comments
            .OrderBy(static comment => comment.Created)
            .ThenBy(static comment => comment.CommentId);
    }

    /// <summary>
    /// The join is what carries the e-mail — <see cref="FileVersion"/> has no <c>User</c> navigation.
    /// </summary>
    public static IQueryable<ExportedVersionRow> AsExportedVersionRows(this IQueryable<FileVersion> versions, IQueryable<User> users)
    {
        return versions.Join(
            users,
            static version => version.UserId,
            static user => user.Id,
            static (version, user) => new ExportedVersionRow
            {
                FileVersionId = version.Id,
                FileId = version.FileAssetId,
                FileName = version.FileAsset!.FileName,
                Number = version.Number,
                SizeBytes = version.SizeBytes,
                MediaType = version.MediaType,
                ContentHash = version.ContentHash,
                Created = version.Created,
                UploaderEmail = user.Email,
                StoragePath = version.StoragePath,
            });
    }

    /// <summary>
    /// By name, as the contact list sorts it; the name claims its document's name in this order.
    /// </summary>
    public static IQueryable<ExportedContact> AsExportedContacts(this IQueryable<Contact> contacts)
    {
        return contacts
            .OrderBy(static contact => EF.Functions.Collate(contact.Name, DatabaseFunctions.CzechCollation))
            .ThenBy(static contact => contact.Id)
            .Select(static contact => new ExportedContact
            {
                ContactId = contact.Id,
                Kind = contact.Kind,
                Name = contact.Name,
                DataBoxId = contact.DataBoxId,
                Address = contact.Address,
                Created = contact.Created,
                Updated = contact.Updated,
            });
    }

    public static IQueryable<ExportedLabel> AsExportedLabels(this IQueryable<Label> labels)
    {
        return labels
            .OrderBy(static label => EF.Functions.Collate(label.Name, DatabaseFunctions.CzechCollation))
            .ThenBy(static label => label.Id)
            .Select(static label => new ExportedLabel { LabelId = label.Id, Name = label.Name, Color = label.Color });
    }
}
