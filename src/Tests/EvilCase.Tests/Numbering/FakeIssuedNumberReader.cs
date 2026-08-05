using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The numbers already stored, in memory, read back through the series itself — so a test that keeps
/// what it issues counts up the way the column does. <see cref="Series"/> is which series each call
/// asked about.
/// </summary>
internal sealed class FakeIssuedNumberReader : IIssuedNumberReader
{
    private readonly List<string> caseNumbers = [];

    private readonly Dictionary<long, List<string>> actNumbers = [];

    public List<string> Series { get; } = [];

    public Task<int> HighestCaseNumber(NumberSeries series, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(series);

        this.Series.Add(series.Prefix);

        return Task.FromResult(series.Highest(this.caseNumbers));
    }

    public Task<int> HighestActNumber(long caseId, NumberSeries series, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(series);

        this.Series.Add(string.Create(CultureInfo.InvariantCulture, $"{caseId}:{series.Prefix}"));

        return Task.FromResult(series.Highest(this.actNumbers.TryGetValue(caseId, out var stored) ? stored : []));
    }

    public string KeepCaseNumber(string number)
    {
        this.caseNumbers.Add(number);

        return number;
    }

    public string KeepActNumber(long caseId, string number)
    {
        if (!this.actNumbers.TryGetValue(caseId, out var stored))
        {
            stored = [];
            this.actNumbers[caseId] = stored;
        }

        stored.Add(number);

        return number;
    }
}
