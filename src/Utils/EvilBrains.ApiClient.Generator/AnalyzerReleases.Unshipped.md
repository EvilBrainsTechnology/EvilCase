; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
;
; EB1010+ are reported only by the source generator; release tracking covers DiagnosticAnalyzers.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EB1001 | EvilBrains.ApiClient | Error | Controller must declare a [Route] attribute
EB1002 | EvilBrains.ApiClient | Error | Action must have exactly one HTTP method attribute with a route template
EB1003 | EvilBrains.ApiClient | Error | Route template must not start with '/' or '~' and must not contain tokens, catch-all or empty placeholders
EB1004 | EvilBrains.ApiClient | Error | Route literal segments must be snake_case
EB1005 | EvilBrains.ApiClient | Error | Action parameter must have exactly one binding attribute or be a CancellationToken
