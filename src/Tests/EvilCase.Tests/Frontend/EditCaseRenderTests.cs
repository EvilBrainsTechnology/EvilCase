using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EditCaseRenderTests
{
    [Test]
    public void TheEditFormRenders()
    {
        using var ctx = new BunitContext();

        var caseId = Guid.CreateVersion7();

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .GetCase(caseId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseDetail
            {
                CaseId = caseId,
                CaseNumber = "EC/20260821-001",
                Date = new DateOnly(2026, 8, 21),
                Title = "Přestupek",
                Status = CaseStatus.Active,
            }));

        var labelsClient = Substitute.For<ILabelsClient>();
        labelsClient
            .ListLabels(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new LabelListResponse { Items = [] }));

        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(labelsClient);
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        // A ValidationMessage outside the page's EditContext throws on render.
        var component = ctx.Render<EditCase>(parameters => parameters.Add(static page => page.CaseId, caseId));

        Assert.That(component.FindComponents<InputText>(), Is.Not.Empty);
    }
}
