using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActRenderTests
{
    [Test]
    public void TheActRenders()
    {
        using var ctx = new BunitContext();

        var actId = Guid.CreateVersion7();

        var detail = new ActDetail
        {
            ActId = actId,
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseStatus = CaseStatus.Active,
            ActNumber = "EC/20260807-001/20260807-002",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        };

        Serve(ctx, actId, detail);

        var component = ctx.Render<Act>(parameters => parameters.Add(static page => page.ActId, actId));

        // A second render diffs the keyed cards again, which threw when two of them shared a key.
        component.Render();

        Assert.That(component.FindComponents<CommentsCard>(), Has.Count.EqualTo(1));
    }

    private static void Serve(BunitContext ctx, Guid actId, ActDetail detail)
    {
        // FilesCard's drop zone imports its own module on first render from a path carrying a
        // version query string, which no SetupModule can name, and reads a property bUnit's
        // runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(detail));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actCommentsClient = Substitute.For<IActCommentsClient>();
        actCommentsClient.ListActComments(actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(Substitute.For<IActLabelsClient>());
        ctx.Services.AddSingleton(actCommentsClient);
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton<IFilePicker>(new StubFilePicker());
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());
    }
}
