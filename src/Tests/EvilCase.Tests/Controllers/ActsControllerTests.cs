using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ActsControllerTests
{
    [Test]
    public async Task FilingWithAContactThatIsNotThereIsAConflict()
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .CreateAct(Arg.Any<CreateActRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ActCreateResult { Outcome = ActCreateOutcome.ContactNotFound });
        var controller = new ActsController();

        var result = await controller.CreateAct(writer, Request(), CancellationToken.None);

        var problem = AssertProblem(result.Result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsAConflict()
    {
        var writer = EditingWriter(ActUpdateOutcome.ContactNotFound);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(ContactProblems.UnknownContact), "an id named in the body is a conflict, not a 404");
    }

    [Test]
    public async Task AnActNumberOutsideTheFormatIsAFieldErrorOnTheNumber()
    {
        var writer = EditingWriter(ActUpdateOutcome.InvalidActNumber);
        var controller = new ActsController();

        var result = await controller.EditAct(writer, Guid.CreateVersion7(), EditRequest(), CancellationToken.None);

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
            Does.ContainKey(nameof(ActEditRequest.ActNumber)),
            "a hand-written number outside the format is reported on the field that carries it");
    }

    private static CreateActRequest Request()
    {
        return new CreateActRequest
        {
            CaseId = Guid.CreateVersion7(),
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            ContactId = Guid.CreateVersion7(),
        };
    }

    private static ActEditRequest EditRequest()
    {
        return new ActEditRequest
        {
            ActNumber = "EC/20260821-001/20260825-001",
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            ContactId = Guid.CreateVersion7(),
        };
    }

    private static IActWriter EditingWriter(ActUpdateOutcome outcome)
    {
        var writer = Substitute.For<IActWriter>();
        writer
            .UpdateAct(Arg.Any<Guid>(), Arg.Any<ActEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
