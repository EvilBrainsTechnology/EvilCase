; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EB0001 | EvilBrains.Style | Warning | Single-line comment should begin with a space
EB0002 | EvilBrains.Style | Warning | Single-line comment should be preceded by a blank line
EB0003 | EvilBrains.Style | Warning | Single-line comment should not be followed by a blank line
EB0004 | EvilBrains.Style | Warning | Using directives should be ordered
EB0005 | EvilBrains.Usage | Warning | ArgumentNullException.ThrowIfNull guards a non-nullable parameter
EB0006 | EvilBrains.Style | Warning | Member should have a block body
