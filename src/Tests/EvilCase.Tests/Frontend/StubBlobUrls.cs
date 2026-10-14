using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never reaches a browser blob; this internal interface cannot be substituted with
/// NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in
/// instead.
/// </summary>
internal sealed class StubBlobUrls : IBlobUrls
{
    public List<(string MediaType, string Url)> Created { get; } = [];

    public List<string> Revoked { get; } = [];

    public Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token)
    {
        var url = $"blob:test/{this.Created.Count}";
        this.Created.Add((mediaType, url));

        return Task.FromResult(url);
    }

    public Task RevokeBlobUrl(string url)
    {
        this.Revoked.Add(url);

        return Task.CompletedTask;
    }
}
