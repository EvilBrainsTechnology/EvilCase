using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcComboboxRenderTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(2);

    [Test]
    public async Task TypingOffersTheMatchesAsOptions()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, static (_, _) => Task.FromResult<IReadOnlyList<string>>(["Úřad práce", "Úřad městské části"]));

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "úřad" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(2)),
            WaitTimeout);
    }

    [Test]
    public async Task SelectingAnOptionRaisesSelectedChanged()
    {
        await using var ctx = new BunitContext();

        string? chosen = null;

        var component = Render(
            ctx,
            static (_, _) => Task.FromResult<IReadOnlyList<string>>(["Úřad práce"]),
            selectedChanged: value => chosen = value);

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "úřad" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)), WaitTimeout);

        await component.Find(".ec-combobox-option").ClickAsync(new MouseEventArgs());

        Assert.That(chosen, Is.EqualTo("Úřad práce"));
    }

    [Test]
    public async Task ASelectedValueRendersReadOnlyAndChangeClearsIt()
    {
        await using var ctx = new BunitContext();

        string? chosen = "Úřad práce";

        var component = Render(
            ctx,
            static (_, _) => Task.FromResult<IReadOnlyList<string>>([]),
            selectedChanged: value => chosen = value,
            selected: "Úřad práce");

        var input = component.Find("input");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(input.GetAttribute("value"), Is.EqualTo("Úřad práce"));
            Assert.That(input.HasAttribute("readonly"), Is.True);
        }

        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.That(chosen, Is.Null);
    }

    [Test]
    public async Task TheCreateRowCarriesTheTypedTextToOnCreate()
    {
        await using var ctx = new BunitContext();

        string? created = null;

        var component = Render(
            ctx,
            static (_, _) => Task.FromResult<IReadOnlyList<string>>([]),
            createText: "Nový úřad",
            onCreate: typed => created = typed);

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "Nový úřad" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-combobox-create"), Has.Count.EqualTo(1)), WaitTimeout);

        await component.Find(".ec-combobox-create").ClickAsync(new MouseEventArgs());

        Assert.That(created, Is.EqualTo("Nový úřad"));
    }

    [Test]
    public async Task EscapeClosesTheMenu()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, static (_, _) => Task.FromResult<IReadOnlyList<string>>(["Úřad práce"]));

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "úřad" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-combobox-option"), Has.Count.EqualTo(1)), WaitTimeout);

        await component.Find("input[type=search]").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(component.FindAll(".ec-combobox-option"), Is.Empty);
    }

    [Test]
    public async Task TheEmptyTextAppearsWhenNothingMatches()
    {
        await using var ctx = new BunitContext();

        var component = Render(ctx, static (_, _) => Task.FromResult<IReadOnlyList<string>>([]));

        await component.Find("input[type=search]").InputAsync(new ChangeEventArgs { Value = "úřad" });

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-field-hint").TextContent, Is.EqualTo("Nic neodpovídá.")),
            WaitTimeout);
    }

    private static IRenderedComponent<EcCombobox<string>> Render(
        BunitContext ctx,
        Func<string, CancellationToken, Task<IReadOnlyList<string>>> search,
        Action<string?>? selectedChanged = null,
        string? selected = null,
        string? createText = null,
        Action<string>? onCreate = null)
    {
        return ctx.Render<EcCombobox<string>>(parameters =>
        {
            parameters
                .Add(static combobox => combobox.InputId, "combobox-input")
                .Add(static combobox => combobox.Placeholder, "Hledat")
                .Add(static combobox => combobox.SelectedLabel, "Vybráno")
                .Add(static combobox => combobox.EmptyText, "Nic neodpovídá.")
                .Add(static combobox => combobox.ItemText, static value => value)
                .Add(static combobox => combobox.Search, search);

            if (selected is not null)
                parameters.Add(static combobox => combobox.Selected, selected);

            if (selectedChanged is not null)
                parameters.Add(static combobox => combobox.SelectedChanged, selectedChanged);

            if (createText is not null)
                parameters.Add(static combobox => combobox.CreateText, createText);

            if (onCreate is not null)
                parameters.Add(static combobox => combobox.OnCreate, onCreate);
        });
    }
}
