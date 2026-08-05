namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberIssuer(
    INumberingSettingsReader settings,
    INumberSequenceAllocator sequences,
    TimeProvider time) : INumberIssuer
{
    public async Task<string> IssueCaseNumber(CancellationToken cancellationToken = default)
    {
        var patterns = await settings.Read(cancellationToken);
        var pattern = Usable(patterns.CaseNumberPattern, NumberPatternKind.CaseNumber);
        var today = this.Today();

        return await this.Issue(pattern, today, $"case:{NumberPattern.PeriodKey(pattern, today)}", caseNumber: null, cancellationToken);
    }

    public async Task<string> IssueActNumber(long caseId, string caseNumber, CancellationToken cancellationToken = default)
    {
        var patterns = await settings.Read(cancellationToken);
        var pattern = Usable(patterns.ActNumberPattern, NumberPatternKind.ActNumber);
        var today = this.Today();
        var scope = string.Create(CultureInfo.InvariantCulture, $"act:{caseId}:{NumberPattern.PeriodKey(pattern, today)}");

        return await this.Issue(pattern, today, scope, caseNumber, cancellationToken);
    }

    private static string Usable(string pattern, NumberPatternKind kind) =>
        NumberPattern.Validate(pattern, kind) is { } error
            ? throw new InvalidOperationException($"the numbering pattern '{pattern}' is refused as {error}")
            : pattern;

    // The zone is the one the application runs in, so a case opened at half past midnight is not
    // numbered yesterday.
    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    private async Task<string> Issue(string pattern, DateOnly today, string scope, string? caseNumber, CancellationToken cancellationToken) =>
        NumberPattern.Format(pattern, today, await sequences.Next(scope, cancellationToken), caseNumber);
}
