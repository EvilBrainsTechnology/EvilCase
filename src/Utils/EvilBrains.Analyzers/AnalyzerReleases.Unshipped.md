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
EB0007 | EvilBrains.Design | Warning | Controller must not take constructor dependencies
EB0008 | EvilBrains.Design | Warning | Action parameters must run [FromServices], [FromRoute], [FromQuery], [FromBody], CancellationToken
EB0009 | EvilBrains.Usage | Warning | Entity is reached through Set<TEntity>() instead of its typed DbSet
EB0010 | EvilBrains.Usage | Warning | Where predicate joins conditions with '&&'
EB0011 | EvilBrains.Usage | Warning | Soft-delete entity is removed by ExecuteDelete
