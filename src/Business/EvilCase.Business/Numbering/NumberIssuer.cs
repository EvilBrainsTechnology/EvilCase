using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Business.Numbering;

internal sealed class NumberIssuer(
    INumberingSettingsReader settings,
    IIssuedNumberReader issued,
    ICaseNumberReader cases,
    TimeProvider time) : INumberIssuer
{
    /// <summary>
    /// A refused number is one another caller had committed by the time the index saw ours, so the read
    /// that follows sees it too and the attempt after it is higher. Every attempt therefore makes
    /// ground — but it makes one caller's worth of it, so the bound covers a burst of them rather than
    /// a single collision.
    /// </summary>
    private const int Attempts = 25;

    public async Task<T> IssueCaseNumber<T>(Func<string, CancellationToken, Task<T>> create, CancellationToken cancellationToken = default)
    {
        var patterns = await settings.Read(cancellationToken);
        var pattern = Usable(patterns.CaseNumberPattern, NumberPatternKind.CaseNumber);
        var today = this.Today();
        var series = NumberPattern.Series(pattern, today);

        return await Issue(pattern, today, caseNumber: null, Case.CaseNumberIndex, token => issued.HighestCaseNumber(series, token), create, cancellationToken);
    }

    public async Task<T> IssueActNumber<T>(long caseId, Func<string, CancellationToken, Task<T>> create, CancellationToken cancellationToken = default)
    {
        var patterns = await settings.Read(cancellationToken);
        var pattern = Usable(patterns.ActNumberPattern, NumberPatternKind.ActNumber);
        var caseNumber = await cases.Read(caseId, cancellationToken);
        var today = this.Today();
        var series = NumberPattern.Series(pattern, today, caseNumber);

        return await Issue(pattern, today, caseNumber, Act.ActNumberIndex, token => issued.HighestActNumber(caseId, series, token), create, cancellationToken);
    }

    private static string Usable(string pattern, NumberPatternKind kind) =>
        NumberPattern.Validate(pattern, kind) is { } error
            ? throw new InvalidOperationException($"the numbering pattern '{pattern}' is refused as {error}")
            : pattern;

    // What the refused attempt left waiting in the change tracker would be written a second time
    // beside the retry's own row.
    private static void Forget(Exception exception)
    {
        if (exception is DbUpdateException { Entries: { } entries })
        {
            foreach (var entry in entries)
                entry.State = EntityState.Detached;
        }
    }

    // The create writes more than the numbered row, and a unique index of its own that refuses one is
    // not the number being taken: retrying it would run the create 25 times over.
    private static bool IsTaken(Exception? exception, string index) => exception switch
    {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation => string.Equals(violation.ConstraintName, index, StringComparison.Ordinal),
        DbUpdateException => IsTaken(exception.InnerException, index),
        _ => false,
    };

    private static async Task<T> Issue<T>(
        string pattern,
        DateOnly today,
        string? caseNumber,
        string index,
        Func<CancellationToken, Task<int>> highest,
        Func<string, CancellationToken, Task<T>> create,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(create);

        for (var attempt = 1; ; attempt++)
        {
            var number = NumberPattern.Format(pattern, today, await highest(cancellationToken) + 1, caseNumber);

            try
            {
                return await create(number, cancellationToken);
            }
            catch (Exception exception) when (attempt < Attempts && IsTaken(exception, index))
            {
                Forget(exception);
            }
        }
    }

    // The zone is the one the application runs in, so a case opened at half past midnight is not
    // numbered yesterday.
    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
