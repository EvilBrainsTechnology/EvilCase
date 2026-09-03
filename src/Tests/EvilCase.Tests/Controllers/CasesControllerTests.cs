using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = Substitute.For<ICaseReader>();
        reader
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns([Item("EC/20260821-002", "druhý"), Item("EC/20260821-001", "první")]);
        var controller = new CasesController();

        var response = await controller.ListCases(reader, new CaseListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Title), Is.EqualTo(["druhý", "první"]), "the controller does not re-order what the reader gave it");
    }

    [Test]
    public async Task TheCountsAreWhatTheReaderRead()
    {
        var counts = new CaseStatusCounts { Active = 2, WaitingOnAuthority = 1, Closed = 3 };
        var reader = Substitute.For<ICaseReader>();
        reader
            .CountCasesByStatus(Arg.Any<CancellationToken>())
            .Returns(counts);
        var controller = new CasesController();

        var response = await controller.CountCases(reader, CancellationToken.None);

        Assert.That(response, Is.EqualTo(counts), "the counts endpoint answers the counts the reader read, which is what feeds the dashboard tile");
    }

    [Test]
    public async Task TheSearchTermReachesTheReaderUntouched()
    {
        CaseListRequest? listRequest = null;
        var reader = Substitute.For<ICaseReader>();
        reader
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns([])
            .AndDoes(call => listRequest = call.Arg<CaseListRequest>());
        var controller = new CasesController();

        await controller.ListCases(reader, new CaseListRequest { Search = "odvolání", Status = CaseStatusFilter.WaitingOnAuthority }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(listRequest?.Search, Is.EqualTo("odvolání"), "the controller decides nothing about the term");
            Assert.That(listRequest?.Status, Is.EqualTo(CaseStatusFilter.WaitingOnAuthority), "the controller hands the status through untouched");
        }
    }

    [Test]
    public async Task TheRequestReachesTheWriterUntouched()
    {
        var writer = CreatingWriter(new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = Item("EC/20260821-001", "Spis") });
        var controller = new CasesController();
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "Popis", ParentCaseId = Guid.CreateVersion7() };

        await controller.CreateCase(writer, request, CancellationToken.None);

        await writer
            .Received(1)
            .CreateCase(request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheCreatedCaseIsWhatTheWriterReturned()
    {
        var created = Item("EC/20260821-001", "Nový spis");
        var writer = CreatingWriter(new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = created });
        var controller = new CasesController();

        var response = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That((response.Result as CreatedAtActionResult)?.Value, Is.SameAs(created), "the created case travels back in the response body");
    }

    [Test]
    public async Task AFiledCaseIsAnsweredWithCreatedAtItsDetailRoute()
    {
        var created = Item("EC/20260821-001", "Nový spis");
        var writer = CreatingWriter(new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = created });
        var controller = new CasesController();

        var response = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis" },
            CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a create answers 201, not 200");
            Assert.That(result.ActionName, Is.EqualTo(nameof(CasesController.GetCase)), "the Location names the detail action of the case");
            Assert.That(result.RouteValues?["caseId"], Is.EqualTo(created.CaseId), "the Location carries the id of the case that was filed");
        }
    }

    [Test]
    public async Task FilingUnderAParentThatIsNoCaseIsAConflict()
    {
        var writer = CreatingWriter(new CaseCreateResult { Outcome = CaseCreateOutcome.InvalidParent });
        var controller = new CasesController();

        var result = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis", ParentCaseId = Guid.CreateVersion7() },
            CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a parent that is no case of the tenant is a conflict the user resolves");
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var caseId = Guid.CreateVersion7();
        var reader = DetailReader(Detail(caseId));
        var controller = new CasesController();

        await controller.GetCase(reader, caseId, CancellationToken.None);

        await reader
            .Received(1)
            .GetCaseDetail(caseId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AMissingCaseIsAProblemWithFourOhFour()
    {
        var controller = new CasesController();

        var result = await controller.GetCase(DetailReader(detail: null), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = EditingWriter(CaseUpdateOutcome.Updated);
        var controller = new CasesController();
        var request = Edit();

        await controller.EditCase(writer, caseId, request, CancellationToken.None);

        await writer
            .Received(1)
            .UpdateCase(caseId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = EditingWriter(CaseUpdateOutcome.Updated);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(CaseUpdateOutcome.NotFound);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ACaseNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = EditingWriter(CaseUpdateOutcome.InvalidCaseNumber);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var problem = (ObjectResult)result;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.StatusCode, Is.EqualTo(400));
            Assert.That(problem.Value, Is.InstanceOf<ValidationProblemDetails>());
        }

        var validation = (ValidationProblemDetails)problem.Value!;

        Assert.That(
            validation.Errors,
            Does.ContainKey(nameof(CaseEditRequest.CaseNumber)),
            "a hand-written number outside the format is reported on the field that carries it");
    }

    [Test]
    public async Task ACaseNumberAnotherCaseHoldsIsAConflict()
    {
        var writer = EditingWriter(CaseUpdateOutcome.CaseNumberTaken);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Detail, Is.Not.Null, "a number another case holds is a conflict the user resolves");
    }

    [Test]
    public async Task AParentThatWouldCloseALoopIsAConflict()
    {
        var writer = EditingWriter(CaseUpdateOutcome.InvalidParent);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo("Invalid parent"), "the edit's two conflicts are told apart by the problem title");
    }

    [Test]
    public async Task DeletingACaseReachesTheWriterWithTheRouteId()
    {
        var caseId = Guid.CreateVersion7();
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new CasesController();

        await controller.DeleteCase(writer, caseId, CancellationToken.None);

        await writer
            .Received(1)
            .DeleteCase(caseId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeletingACaseAnswersWithNoContent()
    {
        var writer = DeletingWriter(DeleteOutcome.Deleted);
        var controller = new CasesController();

        var result = await controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingCaseIsAProblemWithFourOhFour()
    {
        var writer = DeletingWriter(DeleteOutcome.NotFound);
        var controller = new CasesController();

        var result = await controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task ADeleteOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = DeletingWriter((DeleteOutcome)99);
        var controller = new CasesController();

        await Assert.ThatAsync(
            async () => await controller.DeleteCase(writer, Guid.CreateVersion7(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static CaseDetail Detail(Guid caseId)
    {
        return new()
        {
            CaseId = caseId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static CaseEditRequest Edit()
    {
        return new()
        {
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };
    }

    private static CaseListItem Item(string caseNumber, string title)
    {
        return new()
        {
            CaseId = Guid.CreateVersion7(),
            CaseNumber = caseNumber,
            Title = title,
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.Active,
            Changed = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static ICaseReader DetailReader(CaseDetail? detail)
    {
        var reader = Substitute.For<ICaseReader>();
        reader
            .GetCaseDetail(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        return reader;
    }

    private static ICaseWriter CreatingWriter(CaseCreateResult result)
    {
        var writer = Substitute.For<ICaseWriter>();
        writer
            .CreateCase(Arg.Any<CreateCaseRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        return writer;
    }

    private static ICaseWriter EditingWriter(CaseUpdateOutcome outcome)
    {
        var writer = Substitute.For<ICaseWriter>();
        writer
            .UpdateCase(Arg.Any<Guid>(), Arg.Any<CaseEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICaseWriter DeletingWriter(DeleteOutcome outcome)
    {
        var writer = Substitute.For<ICaseWriter>();
        writer
            .DeleteCase(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
