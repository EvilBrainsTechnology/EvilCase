namespace EvilBrains.EvilCase.Business.Numbering;

internal interface INumberingSettingsReader
{
    public Task<NumberingPatterns> Read(CancellationToken cancellationToken = default);
}
