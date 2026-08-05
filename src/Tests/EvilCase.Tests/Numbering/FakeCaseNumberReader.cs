using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The case numbers a test's cases carry, by identifier. An identifier it does not know is a case
/// that is not there.
/// </summary>
internal sealed class FakeCaseNumberReader(params (long CaseId, string CaseNumber)[] cases) : ICaseNumberReader
{
    public Task<string> Read(long caseId, CancellationToken cancellationToken = default) =>
        Array.Find(cases, entry => entry.CaseId == caseId) is { CaseNumber: { } number }
            ? Task.FromResult(number)
            : throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture, $"there is no case {caseId} to number an act under"));
}
