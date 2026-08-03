using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Cases;

/// <summary>
/// Shapes the case list, one composable step per rule, so what the list is can be read in one place and
/// pinned by a test without a server.
/// </summary>
/// <remarks>
/// Every step stays an <see cref="IQueryable{T}"/>: the filtering runs in PostgreSQL rather than over a
/// list the application fetched first, and the projection is what decides which columns are read at all.
/// </remarks>
public static class CaseListQuery
{
    /// <summary>
    /// The character that turns a wildcard back into a literal, so a search for <c>50%</c> does not match
    /// everything.
    /// </summary>
    private const string Escape = "\\";

    /// <summary>
    /// Cases with no parent. The list shows proceedings, not every node in them — a sub-case is reached
    /// through its parent (#99).
    /// </summary>
    public static IQueryable<Case> Roots(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Where(@case => @case.ParentCaseId == null);
    }

    /// <summary>
    /// Narrows to what matches <paramref name="search"/> anywhere in the title or the subject, ignoring
    /// case but not diacritics (#101). A blank term narrows nothing.
    /// </summary>
    public static IQueryable<Case> MatchingSearch(this IQueryable<Case> cases, string? search)
    {
        ArgumentNullException.ThrowIfNull(cases);

        if (string.IsNullOrWhiteSpace(search))
            return cases;

        var pattern = $"%{EscapeWildcards(search.Trim())}%";

        return cases.Where(@case =>
            EF.Functions.ILike(@case.Title, pattern, Escape)
                || (@case.Subject != null && EF.Functions.ILike(@case.Subject, pattern, Escape)));
    }

    /// <summary>
    /// Narrows to what the filter names. <see cref="CaseStatusFilter.Open"/> is everything not closed
    /// and is what a request that says nothing gets (#100); only <see cref="CaseStatusFilter.All"/>
    /// brings the archive back.
    /// </summary>
    public static IQueryable<Case> WithStatus(this IQueryable<Case> cases, CaseStatusFilter filter)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return filter switch
        {
            CaseStatusFilter.Open => cases.Where(@case => @case.Status != CaseStatus.Closed),
            CaseStatusFilter.All => cases,
            CaseStatusFilter.Active => cases.Where(@case => @case.Status == CaseStatus.Active),
            CaseStatusFilter.WaitingOnAuthority => cases.Where(@case => @case.Status == CaseStatus.WaitingOnAuthority),
            CaseStatusFilter.Closed => cases.Where(@case => @case.Status == CaseStatus.Closed),
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown case status filter."),
        };
    }

    /// <summary>
    /// Most recently touched first, which is the order the work is in. The identifier breaks the tie, so
    /// two cases changed in the same instant do not swap places between two calls.
    /// </summary>
    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases
            .OrderByDescending(@case => @case.Updated ?? @case.Created)
            .ThenByDescending(@case => @case.Id);
    }

    /// <summary>
    /// Reads only what a row shows. The tags come with it as one query rather than one per row, and the
    /// sub-case count as a count rather than as the children themselves.
    /// </summary>
    public static IQueryable<CaseListItem> AsListItems(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Select(@case => new CaseListItem
        {
            Id = @case.Id,
            Title = @case.Title,
            Subject = @case.Subject,
            Status = @case.Status,
            Tags = @case.Tags.OrderBy(tag => tag.Value).Select(tag => tag.Value).ToList(),
            Created = @case.Created,
            Updated = @case.Updated,
            SubCaseCount = @case.Children.Count,
        });
    }

    private static string EscapeWildcards(string term) => term
        .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
        .Replace("%", Escape + "%", StringComparison.Ordinal)
        .Replace("_", Escape + "_", StringComparison.Ordinal);
}
