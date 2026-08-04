using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// One counter per scope, in memory. What a test reads from it is which series a number was taken
/// from.
/// </summary>
internal sealed class FakeNumberSequenceAllocator : INumberSequenceAllocator
{
    private readonly Dictionary<string, int> counters = new(StringComparer.Ordinal);

    public List<string> Scopes { get; } = [];

    public Task<int> Next(string scope, CancellationToken cancellationToken = default)
    {
        this.Scopes.Add(scope);
        this.counters[scope] = this.counters.GetValueOrDefault(scope) + 1;

        return Task.FromResult(this.counters[scope]);
    }
}
