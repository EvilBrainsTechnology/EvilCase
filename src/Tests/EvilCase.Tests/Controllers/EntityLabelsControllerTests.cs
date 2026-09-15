using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Labels;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class EntityLabelsControllerTests
{
    [Test]
    public async Task TheCaseRequestReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var writer = CaseWriter(LabelAssignmentOutcome.Assigned);
        var controller = new CaseLabelsController();
        var request = new LabelAssignmentRequest { LabelIds = [Guid.CreateVersion7()] };

        await controller.SetCaseLabels(writer, caseId, request, CancellationToken.None);

        await writer
            .Received(1)
            .SetCaseLabels(caseId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssigningToACaseAnswersWithNoContent()
    {
        var controller = new CaseLabelsController();

        var result = await controller.SetCaseLabels(CaseWriter(LabelAssignmentOutcome.Assigned), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task AssigningToAMissingCaseIsAProblemWithFourOhFour()
    {
        var controller = new CaseLabelsController();

        var result = await controller.SetCaseLabels(CaseWriter(LabelAssignmentOutcome.OwnerNotFound), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AssigningALabelThatIsNotThereIsAConflict()
    {
        var controller = new CaseLabelsController();

        var result = await controller.SetCaseLabels(CaseWriter(LabelAssignmentOutcome.LabelNotFound), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None);

        var problem = AssertProblem(result, 409);

        Assert.That(problem.Title, Is.EqualTo(LabelProblems.UnknownLabel));
    }

    [Test]
    public async Task ACaseOutcomeTheEndpointDoesNotKnowThrows()
    {
        var controller = new CaseLabelsController();

        await Assert.ThatAsync(
            async () => await controller.SetCaseLabels(CaseWriter((LabelAssignmentOutcome)99), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task TheActRequestReachesTheWriterWithBothRouteIdsAndTheBody()
    {
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var writer = ActWriter(LabelAssignmentOutcome.Assigned);
        var controller = new ActLabelsController();
        var request = new LabelAssignmentRequest { LabelIds = [Guid.CreateVersion7()] };

        await controller.SetActLabels(writer, caseId, actId, request, CancellationToken.None);

        await writer
            .Received(1)
            .SetActLabels(caseId, actId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssigningToAMissingActIsAProblemWithFourOhFour()
    {
        var controller = new ActLabelsController();

        var result = await controller.SetActLabels(
            ActWriter(LabelAssignmentOutcome.OwnerNotFound), Guid.CreateVersion7(), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnActOutcomeTheEndpointDoesNotKnowThrows()
    {
        var controller = new ActLabelsController();

        await Assert.ThatAsync(
            async () => await controller.SetActLabels(
                ActWriter((LabelAssignmentOutcome)99), Guid.CreateVersion7(), Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    private static ILabelAssignmentWriter CaseWriter(LabelAssignmentOutcome outcome)
    {
        var writer = Substitute.For<ILabelAssignmentWriter>();
        writer
            .SetCaseLabels(Arg.Any<Guid>(), Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ILabelAssignmentWriter ActWriter(LabelAssignmentOutcome outcome)
    {
        var writer = Substitute.For<ILabelAssignmentWriter>();
        writer
            .SetActLabels(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LabelAssignmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
