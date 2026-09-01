using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Mvc;
using static EvilBrains.EvilCase.Tests.Controllers.ProblemAssertions;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ContactsControllerTests
{
    [Test]
    public async Task TheCreateRequestReachesTheWriterUntouched()
    {
        var writer = CreatingWriter(Item("Kontakt"));
        var controller = new ContactsController();
        var request = new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority };

        await controller.CreateContact(writer, request, CancellationToken.None);

        await writer.Received(1).CreateContact(request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheCreatedContactIsWhatTheWriterReturned()
    {
        var created = Item("Nový kontakt");
        var writer = CreatingWriter(created);
        var controller = new ContactsController();

        var response = await controller.CreateContact(writer, new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority }, CancellationToken.None);

        Assert.That((response.Result as CreatedAtActionResult)?.Value, Is.SameAs(created));
    }

    [Test]
    public async Task AFiledContactIsAnsweredWithCreatedAtItsDetailRoute()
    {
        var created = Item("Nový kontakt");
        var controller = new ContactsController();

        var response = await controller.CreateContact(
            CreatingWriter(created),
            new ContactEditRequest { Name = "Nový kontakt", Kind = ContactKind.Authority },
            CancellationToken.None);

        Assert.That(response.Result, Is.InstanceOf<CreatedAtActionResult>());
        var result = (CreatedAtActionResult)response.Result!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.StatusCode, Is.EqualTo(201), "a create answers 201, not 200");
            Assert.That(result.ActionName, Is.EqualTo(nameof(ContactsController.GetContact)), "the Location names the detail action of the contact");
            Assert.That(result.RouteValues?["contactId"], Is.EqualTo(created.ContactId), "the Location carries the id of the contact that was filed");
        }
    }

    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = ListingReader([]);
        var controller = new ContactsController();
        var request = new ContactListRequest { Search = "úřad" };

        await controller.ListContacts(reader, request, CancellationToken.None);

        await reader.Received(1).ListContacts(request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = ListingReader([Item("Krajský soud ve Vzorově"), Item("Česká advokátní komora")]);
        var controller = new ContactsController();

        var response = await controller.ListContacts(reader, new ContactListRequest(), CancellationToken.None);

        Assert.That(response.Items.Select(static item => item.Name), Is.EqualTo(["Krajský soud ve Vzorově", "Česká advokátní komora"]));
    }

    [Test]
    public async Task TheDefaultContactIsWhatTheReaderReturned()
    {
        var defaultContact = Item("Výchozí kontakt");
        var reader = DefaultContactReader(defaultContact);
        var controller = new ContactsController();

        var result = await controller.GetDefaultContact(reader, CancellationToken.None);

        Assert.That(result, Is.SameAs(defaultContact));
    }

    [Test]
    public async Task TheDetailIsAskedForTheIdInTheRoute()
    {
        var contactId = Guid.CreateVersion7();
        var reader = DetailReader(BuildDetail(contactId));
        var controller = new ContactsController();

        await controller.GetContact(reader, contactId, CancellationToken.None);

        await reader.Received(1).GetContactDetail(contactId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AMissingContactIsAProblemWithFourOhFour()
    {
        var controller = new ContactsController();

        var result = await controller.GetContact(DetailReader(detail: null), Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result.Result, 404);
    }

    [Test]
    public async Task AnEditReachesTheWriterWithTheRouteIdAndTheBody()
    {
        var contactId = Guid.CreateVersion7();
        var writer = EditingWriter(ContactUpdateOutcome.Updated);
        var controller = new ContactsController();
        var request = new ContactEditRequest { Name = "Nový název", Kind = ContactKind.Authority };

        await controller.EditContact(writer, contactId, request, CancellationToken.None);

        await writer.Received(1).UpdateContact(contactId, request, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EditingAMissingContactIsAProblemWithFourOhFour()
    {
        var writer = EditingWriter(ContactUpdateOutcome.NotFound);
        var controller = new ContactsController();

        var result = await controller.EditContact(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    [Test]
    public async Task AnEditThatSucceedsAnswersWithNoContent()
    {
        var writer = EditingWriter(ContactUpdateOutcome.Updated);
        var controller = new ContactsController();

        var result = await controller.EditContact(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task AnEditOutcomeTheEndpointDoesNotKnowThrows()
    {
        var writer = EditingWriter((ContactUpdateOutcome)99);
        var controller = new ContactsController();

        await Assert.ThatAsync(
            async () => await controller.EditContact(writer, Guid.CreateVersion7(), Edit(), CancellationToken.None),
            Throws.InstanceOf<UnreachableException>(),
            "an outcome the endpoint does not name never turns into a status");
    }

    [Test]
    public async Task DeletingAnUnreferencedContactAnswersWithNoContent()
    {
        var writer = DeletingWriter(ContactDeleteOutcome.Deleted);
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeletingAReferencedContactIsAConflict()
    {
        var writer = DeletingWriter(ContactDeleteOutcome.Referenced);
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 409);
    }

    [Test]
    public async Task DeletingTheDefaultContactIsAConflict()
    {
        var referenced = DeletingWriter(ContactDeleteOutcome.Referenced);
        var defaultContact = DeletingWriter(ContactDeleteOutcome.DefaultContact);
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
        var writer = DeletingWriter(ContactDeleteOutcome.NotFound);
        var controller = new ContactsController();

        var result = await controller.DeleteContact(writer, Guid.CreateVersion7(), CancellationToken.None);

        AssertProblem(result, 404);
    }

    private static ContactListItem Item(string name)
    {
        return new() { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = name };
    }

    private static ContactDetail BuildDetail(in Guid contactId)
    {
        return new() { ContactId = contactId, Kind = ContactKind.Authority, Name = "Kontakt" };
    }

    private static ContactEditRequest Edit()
    {
        return new() { Name = "Kontakt", Kind = ContactKind.Authority };
    }

    private static IContactReader ListingReader(IReadOnlyList<ContactListItem> items)
    {
        var reader = Substitute.For<IContactReader>();
        reader.ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(items);

        return reader;
    }

    private static IContactReader DefaultContactReader(ContactListItem contact)
    {
        var reader = Substitute.For<IContactReader>();
        reader.GetDefaultContact(Arg.Any<CancellationToken>())
            .Returns(contact);

        return reader;
    }

    private static IContactReader DetailReader(ContactDetail? detail)
    {
        var reader = Substitute.For<IContactReader>();
        reader.GetContactDetail(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(detail);

        return reader;
    }

    private static IContactWriter CreatingWriter(ContactListItem created)
    {
        var writer = Substitute.For<IContactWriter>();
        writer.CreateContact(Arg.Any<ContactEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(created);

        return writer;
    }

    private static IContactWriter EditingWriter(ContactUpdateOutcome outcome)
    {
        var writer = Substitute.For<IContactWriter>();
        writer.UpdateContact(Arg.Any<Guid>(), Arg.Any<ContactEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }

    private static IContactWriter DeletingWriter(ContactDeleteOutcome outcome)
    {
        var writer = Substitute.For<IContactWriter>();
        writer.DeleteContact(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(outcome);

        return writer;
    }
}
