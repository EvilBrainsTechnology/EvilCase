using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Search;

/// <summary>
/// The steps of the search: each entity is narrowed to the rows a <c>LIKE</c> pattern matches in the
/// fields its row shows, ignoring case and diacritics.
/// </summary>
internal static class SearchQuery
{
    public static IQueryable<Case> MatchingTerm(this IQueryable<Case> cases, string pattern)
    {
        return cases.Where(@case =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(@case.Title), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || EF.Functions.ILike(DatabaseFunctions.Unaccent(@case.CaseNumber), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (@case.ExternalCaseNumber != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(@case.ExternalCaseNumber), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Act> MatchingTerm(this IQueryable<Act> acts, string pattern)
    {
        return acts.Where(act =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(act.Title), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || EF.Functions.ILike(DatabaseFunctions.Unaccent(act.ActNumber), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (act.ExternalActNumber != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(act.ExternalActNumber), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Contact> MatchingTerm(this IQueryable<Contact> contacts, string pattern)
    {
        return contacts.Where(contact =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.Name), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)
                || (contact.Address != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.Address), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape))
                || (contact.DataBoxId != null
                    && EF.Functions.ILike(DatabaseFunctions.Unaccent(contact.DataBoxId), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape)));
    }

    public static IQueryable<FileAsset> MatchingTerm(this IQueryable<FileAsset> files, string pattern)
    {
        return files.Where(file =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(file.FileName), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape));
    }

    public static IQueryable<Comment> MatchingTerm(this IQueryable<Comment> comments, string pattern)
    {
        return comments.Where(comment =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(comment.Body), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape));
    }

    public static IQueryable<Label> MatchingTerm(this IQueryable<Label> labels, string pattern)
    {
        return labels.Where(label =>
            EF.Functions.ILike(DatabaseFunctions.Unaccent(label.Name), DatabaseFunctions.Unaccent(pattern), LikeExtensions.LikeEscape));
    }

    public static IQueryable<Case> InSearchOrder(this IQueryable<Case> cases)
    {
        return cases
            .OrderByDescending(static @case => @case.Date)
            .ThenByDescending(static @case => @case.Created)
            .ThenByDescending(static @case => @case.Id);
    }

    public static IQueryable<Act> InSearchOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderByDescending(static act => act.Date)
            .ThenByDescending(static act => act.Created)
            .ThenByDescending(static act => act.Id);
    }

    public static IQueryable<Contact> InSearchOrder(this IQueryable<Contact> contacts)
    {
        return contacts
            .OrderBy(static contact => contact.Name)
            .ThenBy(static contact => contact.Id);
    }

    public static IQueryable<FileAsset> InSearchOrder(this IQueryable<FileAsset> files)
    {
        return files
            .OrderByDescending(static file => file.Created)
            .ThenByDescending(static file => file.Id);
    }

    public static IQueryable<Comment> InSearchOrder(this IQueryable<Comment> comments)
    {
        return comments
            .OrderByDescending(static comment => comment.Created)
            .ThenByDescending(static comment => comment.Id);
    }

    public static IQueryable<FileSearchItem> AsSearchItems(this IQueryable<FileAsset> files)
    {
        return files.Select(static file => new FileSearchItem
        {
            FileId = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            CaseId = file.CaseId,
            ActId = file.ActId,
            OwnerTitle = file.CaseId != null ? file.Case!.Title : file.Act!.Title,
        });
    }

    public static IQueryable<CommentSearchItem> AsSearchItems(this IQueryable<Comment> comments)
    {
        return comments.Select(static comment => new CommentSearchItem
        {
            CommentId = comment.Id,
            Body = comment.Body,
            Created = comment.Created,
            CaseId = comment.CaseId,
            ActId = comment.ActId,
            OwnerTitle = comment.CaseId != null ? comment.Case!.Title : comment.Act!.Title,
        });
    }
}
