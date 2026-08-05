namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Hands out the next value of one series; two callers never get the same one.
/// </summary>
internal interface INumberSequenceAllocator
{
    public Task<int> Next(string scope, CancellationToken cancellationToken = default);
}
