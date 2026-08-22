using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ContactsControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingContactReader();
        var controller = new ContactsController();
        var request = new ContactListRequest { Search = "úřad" };

        await controller.ListContacts(reader, request, CancellationToken.None);

        Assert.That(reader.Request?.Search, Is.EqualTo("úřad"));
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingContactReader { Items = [Item("Krajský soud ve Vzorově"), Item("Česká advokátní komora")] };
        var controller = new ContactsController();

        var response = await controller.ListContacts(reader, new ContactListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Name), Is.EqualTo(["Krajský soud ve Vzorově", "Česká advokátní komora"]));
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var id = Guid.CreateVersion7();
        var reader = new RecordingContactReader { DetailResult = BuildDetail(id) };
        var controller = new ContactsController();

        await controller.GetContact(reader, id, CancellationToken.None);

        Assert.That(reader.DetailId, Is.EqualTo(id));
    }

    [Test]
    public async Task AMissingContactIsAProblemWithFourOhFour()
    {
        var controller = new ContactsController();

        var result = await controller.GetContact(new RecordingContactReader { DetailResult = null }, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var id = Guid.CreateVersion7();
        var writer = new RecordingContactWriter { UpdateOutcome = ContactUpdateOutcome.Updated };
        var controller = new ContactsController();
        var request = new ContactEditRequest { Name = "Nový název", Kind = ContactKind.Authority };

        await controller.EditContact(writer, id, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateId, Is.EqualTo(id));
            Assert.That(writer.UpdateRequest, Is.SameAs(request));
        }
    }

    [Test]
    public async Task EditingAMissingContactIsAProblemWithFourOhFour()
    {
        var writer = new RecordingContactWriter { UpdateOutcome = ContactUpdateOutcome.NotFound };
        var controller = new ContactsController();

        var result = await controller.EditContact(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = new RecordingContactWriter { UpdateOutcome = ContactUpdateOutcome.Updated };
        var controller = new ContactsController();

        var result = await controller.EditContact(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAnUnreferencedContactAnswersWithNoContent()
    {
        var writer = new RecordingContactWriter { DeleteOutcome = ContactDeleteOutcome.Deleted };
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAReferencedContactIsAConflict()
    {
        var writer = new RecordingContactWriter { DeleteOutcome = ContactDeleteOutcome.Referenced };
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 409);
    }

    [Test]
    public async Task DeletingTheDefaultContactIsAConflict()
    {
        var referenced = new RecordingContactWriter { DeleteOutcome = ContactDeleteOutcome.Referenced };
        var defaultContact = new RecordingContactWriter { DeleteOutcome = ContactDeleteOutcome.DefaultContact };
        var controller = new ContactsController();
        var referencedResult = await controller.DeleteContact(referenced, Guid.CreateVersion7(), CancellationToken.None);
        var defaultResult = await controller.DeleteContact(defaultContact, Guid.CreateVersion7(), CancellationToken.None);

        var referencedProblem = AssertProblem(referencedResult, 409);
        var defaultProblem = AssertProblem(defaultResult, 409);

        Assert.That(defaultProblem.Detail, Is.Not.EqualTo(referencedProblem.Detail), "the default contact says why it cannot go");
    }

    [Test]
    public async Task DeletingAMissingContactIsAProblemWithFourOhFour()
    {
        var writer = new RecordingContactWriter { DeleteOutcome = ContactDeleteOutcome.NotFound };
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ProblemDetails AssertProblem(IActionResult? result, in int statusCode)
    {
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = (ObjectResult)result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(statusCode));
            Assert.That(objectResult.Value, Is.InstanceOf<ProblemDetails>());
        }

        return (ProblemDetails)objectResult.Value!;
    }

    private static ContactListItem Item(string name)
    {
        return new() { Id = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = name };
    }

    private static ContactDetail BuildDetail(in Guid id)
    {
        return new() { Id = id, Kind = ContactKind.Authority, Name = "Kontakt" };
    }

    private static ContactEditRequest Edit()
    {
        return new() { Name = "Kontakt", Kind = ContactKind.Authority };
    }

    private sealed class RecordingContactReader : IContactReader
    {
        public ContactListRequest? Request { get; private set; }

        public IReadOnlyList<ContactListItem> Items { get; init; } = [];

        public Guid? DetailId { get; private set; }

        public ContactDetail? DetailResult { get; init; }

        public Task<IReadOnlyList<ContactListItem>> List(ContactListRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }

        public Task<ContactDetail?> Detail(Guid id, CancellationToken cancellationToken = default)
        {
            this.DetailId = id;

            return Task.FromResult(this.DetailResult);
        }
    }

    private sealed class RecordingContactWriter : IContactWriter
    {
        public Guid? UpdateId { get; private set; }

        public ContactEditRequest? UpdateRequest { get; private set; }

        public ContactUpdateOutcome UpdateOutcome { get; init; }

        public ContactDeleteOutcome DeleteOutcome { get; init; }

        public Task<ContactUpdateOutcome> Update(Guid id, ContactEditRequest request, CancellationToken cancellationToken = default)
        {
            this.UpdateId = id;
            this.UpdateRequest = request;

            return Task.FromResult(this.UpdateOutcome);
        }

        public Task<ContactDeleteOutcome> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.DeleteOutcome);
        }
    }
}
