using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

internal sealed class FakeNumberingSettingsReader(string? caseNumberPattern = null, string? actNumberPattern = null) : INumberingSettingsReader
{
    public Task<NumberingPatterns> Read(CancellationToken cancellationToken = default) =>
        Task.FromResult(new NumberingPatterns(
            caseNumberPattern ?? NumberingDefaults.CaseNumberPattern,
            actNumberPattern ?? NumberingDefaults.ActNumberPattern));
}
