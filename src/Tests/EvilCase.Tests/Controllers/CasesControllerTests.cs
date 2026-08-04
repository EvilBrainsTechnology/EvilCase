using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Cases;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class CasesControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingCaseReader();
        var controller = Controller(reader);
        var request = new CaseListRequest { Search = "odvolání", Status = CaseStatusFilter.WaitingOnAuthority };

        _ = await controller.ListCases(request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.Request?.Search, Is.EqualTo("odvolání"));
            Assert.That(reader.Request?.Status, Is.EqualTo(CaseStatusFilter.WaitingOnAuthority));
        }
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingCaseReader { Items = [Item(2, "druhý"), Item(1, "první")] };
        var controller = Controller(reader);

        var response = await controller.ListCases(new CaseListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Title), Is.EqualTo(["druhý", "první"]));
    }

    [Test]
    public async Task AKnownCaseIsAnsweredWithItsDetail()
    {
        var reader = new RecordingCaseReader { Detail = FakeCases.Detail(7, "Přestupek") };
        var controller = Controller(reader);

        var result = await controller.GetCase(7, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.DetailId, Is.EqualTo(7));
            Assert.That((result.Result as OkObjectResult)?.Value, Is.SameAs(reader.Detail));
        }
    }

    [Test]
    public async Task AnUnknownCaseIsNotFoundRatherThanAnEmptyDetail()
    {
        var controller = Controller(new RecordingCaseReader());

        var result = await controller.GetCase(7, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task ACommentIsAddedToTheCaseInTheRoute()
    {
        var writer = new RecordingCaseCommentWriter { Comment = FakeCases.Comment(1, "poznámka") };
        var controller = Controller(new RecordingCaseReader(), writer);

        var result = await controller.AddCaseComment(7, new AddCaseCommentRequest { Body = "poznámka" }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.CaseId, Is.EqualTo(7));
            Assert.That(writer.Request?.Body, Is.EqualTo("poznámka"));
            Assert.That((result.Result as OkObjectResult)?.Value, Is.SameAs(writer.Comment));
        }
    }

    [Test]
    public async Task ACommentOnAnUnknownCaseIsNotFound()
    {
        var controller = Controller(new RecordingCaseReader());

        var result = await controller.AddCaseComment(7, new AddCaseCommentRequest { Body = "poznámka" }, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    private static CasesController Controller(RecordingCaseReader reader, RecordingCaseCommentWriter? writer = null) =>
        new(reader, writer ?? new RecordingCaseCommentWriter());

    private static CaseListItem Item(long id, string title) => new()
    {
        Id = id,
        Title = title,
        Status = CaseStatus.Active,
        Tags = [],
        Created = DateTime.UtcNow,
        SubCaseCount = 0,
    };
}
