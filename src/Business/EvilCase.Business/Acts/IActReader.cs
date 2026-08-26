using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Reads the acts a case holds.
/// </summary>
public interface IActReader
{
    public Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token);
}
