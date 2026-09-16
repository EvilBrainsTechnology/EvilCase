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
using Microsoft.Extensions.DependencyInjection;
using TabBlazor.Services;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseRenderTests
{
    [Test]
    public void EditModeRendersTheTitleSValidationMessageUnderTheSameEditContextAsTheRestOfTheForm()
    {
        using var ctx = new BunitContext();

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
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new CaseListResponse { Items = [] }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient.ListCaseActs(caseId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new ActListResponse { Items = [] }));

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

        // FilesCard binds the card-wide file drop through this module on first render.
        var dropModule = ctx.JSInterop.SetupModule("./js/file-drop.js");
        dropModule.SetupVoid("bindCardDrop", static _ => true).SetVoidResult();
        dropModule.SetupVoid("unbindCardDrop", static _ => true).SetVoidResult();

        // The regression: before the fix, ValidationMessage sat outside the CascadingValue that
        // carries the EditContext and threw a null cascading-parameter exception on this render.
        var component = ctx.Render<Case>(parameters => parameters
            .Add(static page => page.CaseId, caseId)
            .Add(static page => page.Edit, value: true));

        Assert.That(component.Find("#case-title"), Is.Not.Null, "the title input renders inside the edit form");
    }
}
