using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class LabelsControllerTests
{
    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = Substitute.For<ILabelReader>();
        reader.ListLabels(Arg.Any<CancellationToken>()).Returns([Item("Soud"), Item("Priorita")]);
        var controller = new LabelsController();

        var response = await controller.ListLabels(reader, CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Name), Is.EqualTo(["Soud", "Priorita"]));
    }

    [Test]
    public async Task TheCreateRequestReachesTheWriterUntouched()
    {
        var writer = CreatingWriter(new LabelCreateResult { Outcome = LabelCreateOutcome.Created, Label = Item("Soud") });
        var controller = new LabelsController();
        var request = Edit();

        await controller.CreateLabel(writer, request, CancellationToken.None);

        await writer
            .Received(1)
            .CreateLabel(request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ACreatedLabelIsAnsweredWithCreatedAndTheWritersItem()
    {
        var created = Item("Soud");
        var controller = new LabelsController();

        var response = await controller.CreateLabel(
            CreatingWriter(new LabelCreateResult { Outcome = LabelCreateOutcome.Created, Label = created }),
            Edit(),
            CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a create answers 201, not 200");
            Assert.That(result.Value, Is.SameAs(created));
            Assert.That(result.ActionName, Is.EqualTo(nameof(LabelsController.ListLabels)), "a label has no detail route, so the Location names the list");
        }
    }

    [Test]
    public async Task ATakenNameIsAConflictThatSaysWhy()
    {
        var controller = new LabelsController();

        var response = await controller.CreateLabel(
            CreatingWriter(new LabelCreateResult { Outcome = LabelCreateOutcome.NameTaken }),
            Edit(),
            CancellationToken.None);

        var problem = AssertProblem(response.Result, 409);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Title, Is.EqualTo(LabelProblems.NameTaken));
            Assert.That(problem.Detail, Is.Not.Null, "the conflict says why the name cannot be used");
        }
    }

    [Test]
    public async Task ACreateOutcomeTheEndpointDoesNotKnowThrows()
    {
        var controller = new LabelsController();

        await Assert.ThatAsync(
            async () => await controller.CreateLabel(
                CreatingWriter(new LabelCreateResult { Outcome = (LabelCreateOutcome)99 }),
                Edit(),
                CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var labelId = Guid.CreateVersion7();
        var writer = EditingWriter(LabelUpdateOutcome.Updated);
        var controller = new LabelsController();
        var request = Edit();

        await controller.EditLabel(writer, labelId, request, CancellationToken.None);

        await writer
            .Received(1)
            .UpdateLabel(labelId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var controller = new LabelsController();

        var result = await controller.EditLabel(EditingWriter(LabelUpdateOutcome.Updated), Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task EditingAMissingLabelIsAProblemWithFourOhFour()
    {
        var controller = new LabelsController();

        var result = await controller.EditLabel(EditingWriter(LabelUpdateOutcome.NotFound), Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task EditingIntoATakenNameIsAConflict()
    {
        var controller = new LabelsController();

        var result = await controller.EditLabel(EditingWriter(LabelUpdateOutcome.NameTaken), Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 409);
    }

    [Test]
    public async Task AnEditOutcomeTheEndpointDoesNotKnowThrows()
    {
        var controller = new LabelsController();

        await Assert.ThatAsync(
            async () => await controller.EditLabel(EditingWriter((LabelUpdateOutcome)99), Guid.CreateVersion7(), Edit(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task DeletingALabelAnswersWithNoContent()
    {
        var controller = new LabelsController();

        var result = await controller.DeleteLabel(DeletingWriter(DeleteOutcome.Deleted), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAMissingLabelIsAProblemWithFourOhFour()
    {
        var controller = new LabelsController();

        var result = await controller.DeleteLabel(DeletingWriter(DeleteOutcome.NotFound), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static LabelItem Item(string name)
    {
        return new() { LabelId = Guid.CreateVersion7(), Name = name, Color = LabelColor.Blue };
    }

    private static LabelEditRequest Edit()
    {
        return new() { Name = "Soud", Color = LabelColor.Indigo };
    }

    private static ILabelWriter CreatingWriter(LabelCreateResult result)
    {
        var writer = Substitute.For<ILabelWriter>();
        writer
            .CreateLabel(Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(result);

        return writer;
    }

    private static ILabelWriter EditingWriter(LabelUpdateOutcome outcome)
    {
        var writer = Substitute.For<ILabelWriter>();
        writer
            .UpdateLabel(Arg.Any<Guid>(), Arg.Any<LabelEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static ILabelWriter DeletingWriter(DeleteOutcome outcome)
    {
        var writer = Substitute.For<ILabelWriter>();
        writer
            .DeleteLabel(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
