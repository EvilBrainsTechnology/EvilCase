using Refit;

namespace EvilBrains.EvilCase.Api.Client;

public interface IEchoApi
{
    [Post("/echo")]
    public Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken token = default);
}
