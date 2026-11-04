using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class CaseChildrenCardRenderTests
{
    [Test]
    public async Task TheMainFileButtonPreviewsTheChildsMainFileAndReloadsTheCardAfterAChange()
    {
        await using var ctx = FrontendContext.Create();
        var mainFile = new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = "rozhodnuti.pdf",
            SizeBytes = 1024,
            Created = new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc),
            Version = 1,
            VersionCount = 1,
            IsMain = true,
        };
        var withMain = Guid.NewGuid();
        var children = new CaseListResponse
        {
            Items =
            [
                Child(withMain, "Odvolání") with { MainFile = new MainFile { FileId = mainFile.FileId, FileName = mainFile.FileName } },
                Child(Guid.NewGuid(), "Přestupek"),
            ],
            TotalCount = 2,
        };

        var casesClient = Substitute.For<ICasesClient>();
        casesClient.ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(children));

        var caseFilesClient = Substitute.For<ICaseFilesClient>();
        caseFilesClient.ListCaseFiles(withMain, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new FileListResponse { Items = [mainFile] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(caseFilesClient);
        ctx.Services.AddSingleton(Substitute.For<IActFilesClient>());
        ctx.Services.AddSingleton(Substitute.For<IFileVersionsClient>());
        ctx.Services.AddSingleton(Substitute.For<IMainFileClient>());

        var component = ctx.Render<CaseChildrenCard>(static parameters => parameters.Add(static card => card.ParentCaseId, Guid.NewGuid()));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-child-row"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-child-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].QuerySelector("button.ec-main-file")?.GetAttribute("aria-label"), Is.EqualTo("rozhodnuti.pdf"), "the button names the child's main file");
            Assert.That(rows[1].QuerySelector("button.ec-main-file"), Is.Null, "a child without a main file has no button");
        }

        await rows[0].QuerySelector("button.ec-main-file")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindComponents<FilePreviewModal>(), Has.Count.EqualTo(1)));

        Assert.That(component.FindComponent<MainFilePreview>().Instance.OwnerId, Is.EqualTo(withMain), "the preview shows the child's own file");

        await component.Find(".ec-preview-name-row .ec-main-star").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(
            casesClient.ReceivedCalls().Count(static call => string.Equals(call.GetMethodInfo().Name, nameof(ICasesClient.ListCases), StringComparison.Ordinal)),
            Is.EqualTo(2),
            "the card reloads after the change"));
    }

    private static CaseListItem Child(Guid caseId, string title)
    {
        return new CaseListItem
        {
            CaseId = caseId,
            CaseNumber = "EC/20261104-001",
            Title = title,
            Date = new DateOnly(2026, 11, 4),
            Status = CaseStatus.Active,
            Changed = new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc),
        };
    }
}
