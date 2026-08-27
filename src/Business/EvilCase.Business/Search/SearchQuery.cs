using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Search;

/// <summary>
/// Shapes the combined search, one composable step per rule (SDD-014).
/// </summary>
internal static class SearchQuery
{
    public static IQueryable<Case> MatchingTerm(this IQueryable<Case> cases, string term)
    {
        var text = PrefixQuery(term);
        var pattern = $"%{term.EscapeLikeWildcards()}%";

        return cases.Where(@case =>
            EF.Functions.ToTsVector("simple", DatabaseFunctions.Unaccent(@case.Title))
                .Matches(EF.Functions.ToTsQuery("simple", DatabaseFunctions.Unaccent(text)))
                || EF.Functions.ToTsVector("simple", DatabaseFunctions.Unaccent(@case.Description ?? ""))
                    .Matches(EF.Functions.ToTsQuery("simple", DatabaseFunctions.Unaccent(text)))
                || EF.Functions.ILike(@case.CaseNumber, pattern, LikeExtensions.LikeEscape)
                || @case.ExternalCaseNumbers.Any(number => EF.Functions.ILike(number.Value, pattern, LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Act> MatchingTerm(this IQueryable<Act> acts, string term)
    {
        var text = PrefixQuery(term);
        var pattern = $"%{term.EscapeLikeWildcards()}%";

        return acts.Where(act =>
            EF.Functions.ToTsVector("simple", DatabaseFunctions.Unaccent(act.Title))
                .Matches(EF.Functions.ToTsQuery("simple", DatabaseFunctions.Unaccent(text)))
                || EF.Functions.ToTsVector("simple", DatabaseFunctions.Unaccent(act.Description ?? ""))
                    .Matches(EF.Functions.ToTsQuery("simple", DatabaseFunctions.Unaccent(text)))
                || EF.Functions.ILike(act.ActNumber, pattern, LikeExtensions.LikeEscape)
                || act.ExternalActNumbers.Any(number => EF.Functions.ILike(number.Value, pattern, LikeExtensions.LikeEscape)));
    }

    public static IQueryable<Case> InSearchOrder(this IQueryable<Case> cases)
    {
        return cases.OrderByDescending(@case => @case.Date).ThenBy(@case => @case.CaseNumber);
    }

    public static IQueryable<Act> InSearchOrder(this IQueryable<Act> acts)
    {
        return acts.OrderByDescending(act => act.Date).ThenBy(act => act.ActNumber);
    }

    public static IQueryable<SearchResultItem> AsSearchItems(this IQueryable<Case> cases)
    {
        return cases.Select(@case => new SearchResultItem
        {
            Kind = SearchResultKind.Case,
            CaseId = @case.Id,
            Number = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
        });
    }

    public static IQueryable<SearchResultItem> AsSearchItems(this IQueryable<Act> acts)
    {
        return acts.Select(act => new SearchResultItem
        {
            Kind = SearchResultKind.Act,
            CaseId = act.CaseId,
            ActId = act.Id,
            Number = act.ActNumber,
            Title = act.Title,
            Date = act.Date,
        });
    }

    /// <summary>
    /// The term as a prefix tsquery: every run of letters and digits becomes one prefix lexeme and all
    /// of them have to match; anything else separates, so no punctuation reaches <c>to_tsquery</c>.
    /// </summary>
    private static string PrefixQuery(string term)
    {
        char[] characters = [.. term.Select(character => char.IsLetterOrDigit(character) ? character : ' ')];
        var words = new string(characters).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" & ", words.Select(word => word + ":*"));
    }
}
