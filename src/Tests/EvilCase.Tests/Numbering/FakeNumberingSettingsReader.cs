using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

internal sealed class FakeNumberingSettingsReader(string? caseNumberPattern = null, string? actNumberPattern = null) : INumberingSettingsReader
{
    public Task<NumberingSettings> Read(CancellationToken cancellationToken = default) =>
        Task.FromResult(new NumberingSettings
        {
            Id = NumberingSettings.SingletonId,
            CaseNumberPattern = caseNumberPattern ?? NumberingDefaults.CaseNumberPattern,
            ActNumberPattern = actNumberPattern ?? NumberingDefaults.ActNumberPattern,
        });
}
