using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Navigation;

internal sealed class BrowserHistory(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/browser-history.js"), IBrowserHistory
{
    public async Task TrackSteps()
    {
        await this.InvokeVoid("trackSteps");
    }

    public async Task<int> TakeStep(string target)
    {
        return await this.Invoke<int>("takeStep", target);
    }

    public async Task Go(int distance)
    {
        await this.InvokeVoid("go", distance);
    }
}
