using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactDeleteModalTests
{
    [Test]
    public void TheConfirmationNamesTheContactAndTheLimitOnDeleting()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(Substitute.For<IContactsClient>());

        var contact = new ContactDetail { ContactId = Guid.CreateVersion7(), Name = "Městský úřad", Kind = ContactKind.Authority };

        var component = ctx.Render<ContactDeleteModal>(parameters => parameters
            .Add(static modal => modal.Contact, contact)
            .Add(static modal => modal.Open, value: true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Markup, Does.Contain("Městský úřad"));
            Assert.That(component.Markup, Does.Contain("Kontakt, který figuruje ve spisu nebo úkonu, smazat nejde"));
        }
    }

    [Test]
    public async Task AConflictAnswerSaysTheContactIsUsed()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contact = new ContactDetail { ContactId = Guid.CreateVersion7(), Name = "Městský úřad", Kind = ContactKind.Authority };

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .DeleteContact(contact.ContactId, Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException(new ApiException(HttpStatusCode.Conflict, responseBody: null)));

        ctx.Services.AddSingleton(contactsClient);

        var component = ctx.Render<ContactDeleteModal>(parameters => parameters
            .Add(static modal => modal.Contact, contact)
            .Add(static modal => modal.Open, value: true));

        await component.Find(".ec-button-danger").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() =>
            Assert.That(component.Markup, Does.Contain("Kontakt nelze smazat: figuruje ve spisu nebo úkonu.")));
    }
}
