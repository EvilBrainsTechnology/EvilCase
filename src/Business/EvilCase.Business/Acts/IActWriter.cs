using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Writes the acts the user files.
/// </summary>
public interface IActWriter
{
    public Task<ActCreateResult> CreateAct(Guid caseId, CreateActRequest request, CancellationToken token);
}
