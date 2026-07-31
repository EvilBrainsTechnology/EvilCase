using System;
using System.Collections.Immutable;

namespace EvilBrains.ApiClient.Generator;

internal static class RouteTemplate
{
    public static string Combine(string controllerTemplate, string actionTemplate)
    {
        var controller = controllerTemplate.Trim('/');
        var action = actionTemplate.Trim('/');
        if (controller.Length == 0)
            return "/" + action;

        if (action.Length == 0)
            return "/" + controller;

        return "/" + controller + "/" + action;
    }

    public static bool HasForbiddenSyntax(string template) =>
        template.StartsWith("/", StringComparison.Ordinal)
            || template.StartsWith("~", StringComparison.Ordinal)
            || template.IndexOf('[') >= 0
            || template.IndexOf("{*", StringComparison.Ordinal) >= 0
            || template.IndexOf("{}", StringComparison.Ordinal) >= 0;

    public static string? FindNonKebabCaseSegment(string template)
    {
        foreach (var segment in template.Split('/'))
        {
            if (segment.Length == 0 || segment[0] == '{')
                continue;

            foreach (var character in segment)
            {
                if (character is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '-'))
                    return segment;
            }
        }

        return null;
    }

    public static ImmutableArray<string> GetPlaceholders(string route)
    {
        var result = ImmutableArray.CreateBuilder<string>();
        var i = 0;

        while (i < route.Length)
        {
            if (route[i] != '{')
            {
                i++;

                continue;
            }

            var start = i + 1;
            var end = start;
            while (end < route.Length && (char.IsLetterOrDigit(route[end]) || route[end] == '_'))
                end++;

            if (end > start)
                result.Add(route.Substring(start, end - start));

            var close = route.IndexOf('}', end);
            i = close < 0 ? route.Length : close + 1;
        }

        return result.ToImmutable();
    }
}
