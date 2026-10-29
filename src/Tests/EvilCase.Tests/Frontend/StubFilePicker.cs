using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never reaches the browser; this internal interface cannot be substituted with
/// NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in
/// instead.
/// </summary>
internal sealed class StubFilePicker : IFilePicker
{
    public List<ElementReference> Picked { get; } = [];

    public Task PickFile(ElementReference input, CancellationToken token)
    {
        this.Picked.Add(input);

        return Task.CompletedTask;
    }
}
