using AngleSharp.Dom;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseRenderTests
{
    [Test]
    public void TheCaseRenders()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        Serve(ctx, caseId, Detail(caseId));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        // A second render diffs the keyed cards again, which threw when two of them shared a key.
        component.Render();

        Assert.That(component.FindComponents<CommentsCard>(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ALabelAddedWhileTheLastOneIsSavingKeepsIt()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var first = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };
        var second = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };

        var detail = Detail(caseId);

        var (_, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, detail);

        var saving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(first), LabelListItems.Of(second)] }));
        caseLabelsClient
            .SetCaseLabels(caseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(saving.Task, Task.CompletedTask);

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(2));

        var addingFirst = component.FindAll(".ec-label-option")[0].ClickAsync(new MouseEventArgs());

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1), "the added label leaves the offer before its save answers"),
            TimeSpan.FromSeconds(2));

        var addingSecond = component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        saving.SetResult();

        await Task.WhenAll(addingFirst, addingSecond);

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { first.LabelId, second.LabelId })),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LeavingTheCaseMidSaveWritesTheLatestLabelsToThatCaseOnly()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();
        var nextCaseId = Guid.CreateVersion7();

        var first = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "InfZ", Color = LabelColor.Blue };
        var second = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };
        var carried = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Hlídat lhůtu", Color = LabelColor.Orange };

        var (casesClient, caseLabelsClient, _, labelsClient) = Serve(ctx, caseId, Detail(caseId));

        var saving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [LabelListItems.Of(first), LabelListItems.Of(second)] }));
        caseLabelsClient
            .SetCaseLabels(Arg.Any<Guid>(), Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(saving.Task, Task.CompletedTask);
        casesClient
            .GetCase(nextCaseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Detail(nextCaseId) with { Labels = [carried] }));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(2));

        _ = component.FindAll(".ec-label-option")[0].ClickAsync(new MouseEventArgs());

        await component.Find(".ec-label-add").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-option"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(2));

        var addingSecond = component.Find(".ec-label-option").ClickAsync(new MouseEventArgs());

        component.Render(parameters => parameters.Add(static page => page.CaseId, nextCaseId));

        saving.SetResult();
        await addingSecond;

        var chips = await component.WaitForElementsAsync(".ec-label-field-inline > .ec-chip", 1, TimeSpan.FromSeconds(2));

        Assert.That(chips.Single().TextContent, Does.Contain(carried.Name), "the next case shows its own labels");

        await caseLabelsClient
            .Received(1)
            .SetCaseLabels(
                caseId,
                Arg.Is<LabelAssignmentRequest>(request => request.LabelIds.SequenceEqual(new[] { first.LabelId, second.LabelId })),
                Arg.Any<CancellationToken>());
        await caseLabelsClient
            .DidNotReceive()
            .SetCaseLabels(nextCaseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AFailedLabelSaveTakesTheChangeBack()
    {
        await using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var kept = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Priorita", Color = LabelColor.Red };

        var (_, caseLabelsClient, _, _) = Serve(ctx, caseId, Detail(caseId) with { Labels = [kept] });

        caseLabelsClient
            .SetCaseLabels(caseId, Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException()));

        var component = ctx.Render<Case>(parameters => parameters.Add(static page => page.CaseId, caseId));

        await component.Find("button[aria-label='Odebrat štítek Priorita']").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(
            () => Assert.That(component.FindAll(".ec-label-field-inline > .ec-chip"), Has.Count.EqualTo(1), "the label the save did not take off returns"),
            TimeSpan.FromSeconds(2));
    }

    private static CaseDetail Detail(Guid caseId)
    {
        return new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static (ICasesClient Cases, ICaseLabelsClient CaseLabels, ICaseCommentsClient CaseComments, ILabelsClient Labels) Serve(
        BunitContext ctx,
        Guid caseId,
        CaseDetail detail)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.GetCase(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var caseLabelsClient = Substitute.For<ICaseLabelsClient>();

        var caseCommentsClient = Substitute.For<ICaseCommentsClient>();
        caseCommentsClient
            .ListCaseComments(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(caseLabelsClient);
        ctx.Services.AddSingleton(caseCommentsClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton<IFilePicker>(new StubFilePicker());
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        return (casesClient, caseLabelsClient, caseCommentsClient, labelsClient);
    }
}
