using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace EvilBrains.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArgumentNullGuardAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor GuardsNonNullable = new(
        "EB0005",
        "ArgumentNullException.ThrowIfNull guards a non-nullable parameter",
        "ArgumentNullException.ThrowIfNull guards '{0}', which the nullable context already keeps non-null",
        "EvilBrains.Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [GuardsNonNullable];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        if (!IsThrowIfNull(invocation.TargetMethod) || invocation.Arguments.Length == 0)
            return;

        if (Unwrap(invocation.Arguments[0].Value) is not IParameterReferenceOperation reference)
            return;

        var parameter = reference.Parameter;

        if (!parameter.Type.IsReferenceType || parameter.NullableAnnotation != NullableAnnotation.NotAnnotated)
            return;

        context.ReportDiagnostic(Diagnostic.Create(GuardsNonNullable, invocation.Syntax.GetLocation(), parameter.Name));
    }

    private static bool IsThrowIfNull(IMethodSymbol method) =>
        string.Equals(method.Name, "ThrowIfNull", StringComparison.Ordinal)
            && string.Equals(method.ContainingType?.ToDisplayString(), "System.ArgumentNullException", StringComparison.Ordinal);

    private static IOperation Unwrap(IOperation operation) =>
        operation is IConversionOperation conversion ? Unwrap(conversion.Operand) : operation;
}
