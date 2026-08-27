using EvilBrains.EvilCase.Api.Contract.Search;

namespace EvilBrains.EvilCase.App.Search;

public static class SearchResultDisplay
{
    public static string KindText(SearchResultKind kind)
    {
        return kind switch
        {
            SearchResultKind.Case => "Spis",
            SearchResultKind.Act => "Úkon",
            _ => "",
        };
    }

    public static string Href(SearchResultItem item)
    {
        return item.ActId is null ? $"/cases/{item.CaseId}" : $"/cases/{item.CaseId}/act/{item.ActId}";
    }
}
