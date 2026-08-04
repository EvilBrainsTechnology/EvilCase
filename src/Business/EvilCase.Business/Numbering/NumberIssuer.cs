namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberIssuer(
    INumberingSettingsReader settings,
    INumberSequenceAllocator sequences,
    TimeProvider time) : INumberIssuer
{
    public async Task<string> IssueCaseNumber(CancellationToken cancellationToken = default)
    {
        var pattern = (await settings.Read(cancellationToken)).CaseNumberPattern;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        return await this.Issue(pattern, today, $"case:{NumberPattern.PeriodKey(pattern, today)}", caseNumber: null, cancellationToken);
    }

    public async Task<string> IssueActNumber(long caseId, string caseNumber, CancellationToken cancellationToken = default)
    {
        var pattern = (await settings.Read(cancellationToken)).ActNumberPattern;
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var scope = string.Create(CultureInfo.InvariantCulture, $"act:{caseId}:{NumberPattern.PeriodKey(pattern, today)}");

        return await this.Issue(pattern, today, scope, caseNumber, cancellationToken);
    }

    // A pattern naming no {seq} counts nothing, so it takes no value from the series either.
    private async Task<string> Issue(string pattern, DateOnly today, string scope, string? caseNumber, CancellationToken cancellationToken)
    {
        var sequence = NumberPattern.NamesSequence(pattern)
            ? await sequences.Next(scope, cancellationToken)
            : 0;

        return NumberPattern.Format(pattern, today, sequence, caseNumber);
    }
}
