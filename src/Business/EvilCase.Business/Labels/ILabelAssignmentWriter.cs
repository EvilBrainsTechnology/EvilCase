using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.Business.Labels;

public interface ILabelAssignmentWriter
{
    public Task<LabelAssignmentOutcome> SetCaseLabels(Guid caseId, LabelAssignmentRequest request, CancellationToken token);

    public Task<LabelAssignmentOutcome> SetActLabels(Guid caseId, Guid actId, LabelAssignmentRequest request, CancellationToken token);
}
