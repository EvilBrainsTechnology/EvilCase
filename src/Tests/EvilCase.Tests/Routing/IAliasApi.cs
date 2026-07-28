using Refit;

namespace EvilBrains.EvilCase.Tests.Routing;

public interface IAliasApi
{
    [Get("/cases/{caseId}")]
    public Task<string> GetAsync([AliasAs("caseId")] Guid id, CancellationToken token = default);
}
