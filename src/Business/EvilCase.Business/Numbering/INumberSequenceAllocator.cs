namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Hands out the next value of one series and never the same one twice.
/// </summary>
internal interface INumberSequenceAllocator
{
    public Task<int> Next(string scope, CancellationToken cancellationToken = default);
}
