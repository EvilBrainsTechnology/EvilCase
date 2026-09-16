using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseRenderTests
{
    [Test]
    public void EditModeRendersTheTitleSValidationMessageUnderTheSameEditContextAsTheRestOfTheForm()
    {
        using var ctx = new BunitContext();

        // FilesCard's drop zone imports its own module on first render from a path carrying a
        // version query string, which no SetupModule can name, and reads a property bUnit's
        // runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var caseId = Guid.CreateVersion7();

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.GetCase(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            Date = new DateOnly(2026, 8, 7),
            Title = "Spis",
            Status = CaseStatus.Active,
        }));
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var caseCommentsClient = Substitute.For<ICaseCommentsClient>();
        caseCommentsClient.ListCaseComments(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(Substitute.For<ICaseLabelsClient>());
        ctx.Services.AddSingleton(caseCommentsClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(Substitute.For<IModalService>());
        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        // The regression: before the fix, ValidationMessage sat outside the CascadingValue that
        // carries the EditContext and threw a null cascading-parameter exception on this render.
        var component = ctx.Render<Case>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.Edit, value: true));

        Assert.That(component.Find("#case-title"), Is.Not.Null, "the title input renders inside the edit form");
    }

    [Test]
    public void PickingALabelUpdatesTheScreenWithoutAReload()
    {
        using var ctx = new BunitContext();

        // FilesCard's drop zone imports its own module on first render from a path carrying a
        // version query string, which no SetupModule can name, and reads a property bUnit's
        // runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var caseId = Guid.CreateVersion7();
        var label = new LabelItem { LabelId = Guid.CreateVersion7(), Name = "Urgentní", Color = LabelColor.Red };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.GetCase(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CaseDetail
        {
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            Date = new DateOnly(2026, 8, 7),
            Title = "Spis",
            Status = CaseStatus.Active,
        }));
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [label] }));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var caseCommentsClient = Substitute.For<ICaseCommentsClient>();
        caseCommentsClient.ListCaseComments(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(Substitute.For<ICaseLabelsClient>());
        ctx.Services.AddSingleton(caseCommentsClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(Substitute.For<IModalService>());
        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        // The regression: FilesCard and CommentsCard shared the same @key, so this second render
        // threw from RenderTreeDiffBuilder and the whole batch was discarded.
        var component = ctx.Render<Case>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.Edit, value: true));

        component.Find("#case-labels").Focus();
        component.Find("div.ec-picker-menu button.dropdown-item").Click();

        Assert.That(component.Find("div.badges-list span.badge").TextContent, Does.Contain(label.Name), "the picked label's badge renders at once");
    }
}
