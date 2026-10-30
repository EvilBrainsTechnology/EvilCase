using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
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

        Assert.That(problem.Title, Is.EqualTo(CaseProblems.InvalidParent), "the create's conflicts are told apart by the problem title");
    }

    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAConflict()
    {
        var writer = CreatingWriter(new CaseCreateResult { Outcome = CaseCreateOutcome.ContactNotFound });
        var controller = new CasesController();

        var result = await controller.CreateCase(
            writer,
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Nový spis", ContactId = Guid.CreateVersion7() },
            CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "the create's conflicts are told apart by the problem title");
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
    public async Task AParentThatWouldCloseALoopIsAConflict()
    {
        var writer = EditingWriter(CaseUpdateOutcome.InvalidParent);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(CaseProblems.InvalidParent), "the edit page tells this conflict apart by the problem title");
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsAConflict()
    {
        var writer = EditingWriter(CaseUpdateOutcome.ContactNotFound);
        var controller = new CasesController();

        var result = await controller.EditCase(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "the edit page tells this conflict apart by the problem title");
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

    private static ICaseWriter EditingWriter(CaseUpdateOutcome outcome)
    {
        var writer = Substitute.For<ICaseWriter>();
        writer
            .UpdateCase(Arg.Any<Guid>(), Arg.Any<CaseEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ICaseWriter CreatingWriter(CaseCreateResult result)
    {
        var writer = Substitute.For<ICaseWriter>();
        writer
            .CreateCase(Arg.Any<CreateCaseRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        return writer;
    }
}
