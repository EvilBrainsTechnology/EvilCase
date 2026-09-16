using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ActRenderTests
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
        var actId = Guid.CreateVersion7();

        var actsClient = Substitute.For<IActsClient>();
        actsClient.GetAct(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ActDetail
        {
            ActId = actId,
            CaseId = caseId,
            CaseNumber = "EC/20260807-001",
            CaseTitle = "Spis",
            CaseDate = new DateOnly(2026, 8, 7),
            CaseStatus = CaseStatus.Active,
            ActNumber = "1",
            Date = new DateOnly(2026, 8, 7),
            Title = "Úkon",
        }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient.ListLabels(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        var actFilesClient = Substitute.For<IActFilesClient>();
        actFilesClient.ListActFiles(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [] }));

        var actCommentsClient = Substitute.For<IActCommentsClient>();
        actCommentsClient.ListActComments(caseId, actId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CommentListResponse { Items = [] }));

        ctx.Services.AddSingleton(actsClient);
        ctx.Services.AddSingleton(Substitute.For<IActLabelsClient>());
        ctx.Services.AddSingleton(actCommentsClient);
        ctx.Services.AddSingleton(actFilesClient);
        ctx.Services.AddSingleton<IFileTransferClient>(new StubFileTransferClient());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(Substitute.For<IModalService>());
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        // The regression: before the fix, ValidationMessage sat outside the CascadingValue that
        // carries the EditContext and threw a null cascading-parameter exception on this render.
        var component = ctx.Render<Act>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.ActId, actId)
            .Add(static page => page.Edit, value: true));

        Assert.That(component.Find("#act-title"), Is.Not.Null, "the title input renders inside the edit form");
    }
}
