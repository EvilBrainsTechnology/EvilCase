using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Numbering;

public interface INumberingSettingsReader
{
    public Task<NumberingSettings> Read(CancellationToken cancellationToken = default);
}
