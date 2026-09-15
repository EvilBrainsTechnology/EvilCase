using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.Business.Labels;

public interface ILabelReader
{
    public Task<IReadOnlyList<LabelItem>> ListLabels(CancellationToken token);
}
