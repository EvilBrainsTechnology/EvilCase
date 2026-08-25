using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ContactsControllerTests
{
    [Test]
    public async Task TheCreateRequestReachesTheWriterUntouched()
    {
        var writer = new RecordingContactWriter();
        var controller = new ContactsController();
        var request = new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority };

        await controller.CreateContact(writer, request, CancellationToken.None);

        Assert.That(writer.CreateRequest, Is.SameAs(request));
    }

    [Test]
    public async Task TheCreatedContactIsWhatTheWriterReturned()
    {
        var created = Item("Nový kontakt");
        var writer = new RecordingContactWriter { Created = created };
        var controller = new ContactsController();

        var result = await controller.CreateContact(writer, new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority }, CancellationToken.None);

        Assert.That(result, Is.SameAs(created));
    }

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
    public async Task TheDefaultContactIsWhatTheReaderReturned()
    {
        var defaultContact = Item("Výchozí kontakt");
        var reader = new RecordingContactReader { DefaultContact = defaultContact };
        var controller = new ContactsController();

        var result = await controller.GetDefaultContact(reader, CancellationToken.None);

        Assert.That(result, Is.SameAs(defaultContact));
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var contactId = Guid.CreateVersion7();
        var reader = new RecordingContactReader { DetailResult = BuildDetail(contactId) };
        var controller = new ContactsController();

        await controller.GetContact(reader, contactId, CancellationToken.None);

        Assert.That(reader.DetailId, Is.EqualTo(contactId));
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
        var contactId = Guid.CreateVersion7();
        var writer = new RecordingContactWriter { UpdateOutcome = ContactUpdateOutcome.Updated };
        var controller = new ContactsController();
        var request = new ContactEditRequest { Name = "Nový název", Kind = ContactKind.Authority };

        await controller.EditContact(writer, contactId, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(writer.UpdateId, Is.EqualTo(contactId));
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

    private static ContactDetail BuildDetail(in Guid contactId)
    {
        return new() { Id = contactId, Kind = ContactKind.Authority, Name = "Kontakt" };
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

        public ContactListItem DefaultContact { get; init; } = Item("Výchozí kontakt");

        public Task<IReadOnlyList<ContactListItem>> ListContacts(ContactListRequest request, CancellationToken token)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }

        public Task<ContactDetail?> GetContactDetail(Guid contactId, CancellationToken token)
        {
            this.DetailId = contactId;

            return Task.FromResult(this.DetailResult);
        }

        public Task<ContactListItem> GetDefaultContact(CancellationToken token)
        {
            return Task.FromResult(this.DefaultContact);
        }
    }

    private sealed class RecordingContactWriter : IContactWriter
    {
        public ContactEditRequest? CreateRequest { get; private set; }

        public ContactListItem Created { get; init; } = Item("Kontakt");

        public Guid? UpdateId { get; private set; }

        public ContactEditRequest? UpdateRequest { get; private set; }

        public ContactUpdateOutcome UpdateOutcome { get; init; }

        public ContactDeleteOutcome DeleteOutcome { get; init; }

        public Task<ContactListItem> CreateContact(ContactEditRequest request, CancellationToken token)
        {
            this.CreateRequest = request;

            return Task.FromResult(this.Created);
        }

        public Task<ContactUpdateOutcome> UpdateContact(Guid contactId, ContactEditRequest request, CancellationToken token)
        {
            this.UpdateId = contactId;
            this.UpdateRequest = request;

            return Task.FromResult(this.UpdateOutcome);
        }

        public Task<ContactDeleteOutcome> DeleteContact(Guid contactId, CancellationToken token)
        {
            return Task.FromResult(this.DeleteOutcome);
        }
    }
}
