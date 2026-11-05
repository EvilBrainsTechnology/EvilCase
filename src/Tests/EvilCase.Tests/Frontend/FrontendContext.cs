using Bunit;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A bUnit context with loose JS interop and a stub behind every browser service; a test reads a
/// stub back by its own type from <c>Services</c>.
/// </summary>
internal static class FrontendContext
{
    public static BunitContext Create()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        AddStub<IBrowserFiles, StubBrowserFiles>(ctx);
        AddStub<IFilePreviews, StubFilePreviews>(ctx);
        AddStub<ITextEditors, StubTextEditors>(ctx);
        AddStub<ITextCompares, StubTextCompares>(ctx);
        AddStub<IMarkdownEditors, StubMarkdownEditors>(ctx);
        AddStub<IFileTransferClient, StubFileTransferClient>(ctx);
        AddStub<IBrowserHistory, StubBrowserHistory>(ctx);
        AddStub<IClipboardWriter, StubClipboardWriter>(ctx);
        AddStub<IAuthSession, StubAuthSession>(ctx);
        ctx.Services.AddSingleton<LazyAssemblyLoader>();

        return ctx;
    }

    private static void AddStub<TService, TStub>(BunitContext ctx)
        where TService : class
        where TStub : class, TService
    {
        ctx.Services.AddSingleton<TStub>();
        ctx.Services.AddSingleton<TService>(static services => services.GetRequiredService<TStub>());
    }
}
