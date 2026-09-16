using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// FileDropZone reads <c>navigator.onLine</c> on first render, and bUnit's runtime leaves that
/// read to the interface's own default body, which throws. A render test that puts
/// <c>FilesCard</c> on the page registers this over bUnit's runtime to answer it.
/// </summary>
internal sealed class PropertyReadingJSRuntime(IJSRuntime inner) : IJSRuntime
{
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return await inner.InvokeAsync<TValue>(identifier, args);
    }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return await inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    public async ValueTask<TValue> GetValueAsync<TValue>(string identifier)
    {
        return await this.GetValueAsync<TValue>(identifier, CancellationToken.None);
    }

    public ValueTask<TValue> GetValueAsync<TValue>(string identifier, CancellationToken cancellationToken)
    {
        // The test browser is online; a read of anything else fails on the cast rather than
        // quietly answering with a default.
        return ValueTask.FromResult((TValue)(object)true);
    }
}
