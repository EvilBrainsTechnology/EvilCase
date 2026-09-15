using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;

namespace EvilBrains.EvilCase.Business.Labels;

public interface ILabelWriter
{
    public Task<LabelCreateResult> CreateLabel(LabelEditRequest request, CancellationToken token);

    public Task<LabelUpdateOutcome> UpdateLabel(Guid labelId, LabelEditRequest request, CancellationToken token);

    public Task<DeleteOutcome> DeleteLabel(Guid labelId, CancellationToken token);
}
