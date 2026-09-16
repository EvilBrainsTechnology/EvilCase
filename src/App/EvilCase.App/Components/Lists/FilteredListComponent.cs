using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Contract.Lists;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Components.Lists;

public abstract class FilteredListComponent<TSortRequest> : ListComponent<TSortRequest>
    where TSortRequest : ListRequest
{
    protected DateOnly? From { get; private set; }

    protected DateOnly? To { get; private set; }

    protected IReadOnlyList<LabelItem> SelectedLabels { get; private set; } = [];

    protected IReadOnlyList<Guid> LabelIds => [.. this.SelectedLabels.Select(static label => label.LabelId)];

    protected async Task OnFromInput(ChangeEventArgs args)
    {
        this.From = ParseDate(args);

        await this.ReloadFirstPage();
    }

    protected async Task OnToInput(ChangeEventArgs args)
    {
        this.To = ParseDate(args);

        await this.ReloadFirstPage();
    }

    protected async Task OnLabelsChanged(IReadOnlyList<LabelItem> labels)
    {
        this.SelectedLabels = labels;

        await this.ReloadFirstPage();
    }

    private static DateOnly? ParseDate(ChangeEventArgs args)
    {
        return DateOnly.TryParse(args.Value as string, CultureInfo.InvariantCulture, out var date) ? date : null;
    }
}
