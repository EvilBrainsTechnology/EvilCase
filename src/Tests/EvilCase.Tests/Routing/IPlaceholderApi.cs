using Refit;

namespace EvilBrains.EvilCase.Tests.Routing;

public interface IPlaceholderApi
{
    [Get("/items/{itemId}")]
    public Task<string> GetAsync(int id, CancellationToken token = default);
}
