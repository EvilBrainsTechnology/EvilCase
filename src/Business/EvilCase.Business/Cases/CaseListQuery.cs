using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Shapes the case list, one composable step per rule.
/// </summary>
public static class CaseListQuery
{
    /// <summary>
    /// Turns a wildcard in the search term back into a literal.
    /// </summary>
    private const string Escape = "\\";

    /// <summary>
    /// Matches the title or the subject, ignoring case but not diacritics. A blank term narrows nothing.
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
    /// Most recently touched first, the identifier breaking the tie so the order is total.
    /// </summary>
    public static IQueryable<Case> InListOrder(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases
            .OrderByDescending(@case => @case.Updated ?? @case.Created)
            .ThenByDescending(@case => @case.Id);
    }

    /// <summary>
    /// Reads only what a row shows, in one query.
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
        });
    }

    private static string EscapeWildcards(string term) => term
        .Replace(Escape, Escape + Escape, StringComparison.Ordinal)
        .Replace("%", Escape + "%", StringComparison.Ordinal)
        .Replace("_", Escape + "_", StringComparison.Ordinal);
}
