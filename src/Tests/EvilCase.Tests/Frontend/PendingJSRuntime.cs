using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Answers an import with itself and holds every other call until <see cref="Complete"/>; like the
/// browser runtime, a cancelled token abandons the held call.
/// </summary>
internal sealed class PendingJSRuntime : IJSRuntime, IJSObjectReference
{
    private readonly TaskCompletionSource<object?> result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Complete(object? value)
    {
        this.result.SetResult(value);
    }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        return await this.InvokeAsync<TValue>(identifier, CancellationToken.None, args);
    }

    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (string.Equals(identifier, "import", StringComparison.Ordinal))
            return (TValue)(object)this;

        return (TValue)(await this.result.Task.WaitAsync(cancellationToken))!;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
