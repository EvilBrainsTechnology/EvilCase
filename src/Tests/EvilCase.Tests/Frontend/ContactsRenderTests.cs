using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactsRenderTests
{
    [Test]
    public void ThePageHeaderCountsTheContactsTheListFound()
    {
        using var ctx = new BunitContext();
        Serve(ctx, total: 11);

        var component = ctx.Render<App.Pages.Contacts>();

        component.WaitForAssertion(() => Assert.That(component.Markup, Does.Contain("11 záznamů")));
    }

    [Test]
    public async Task TheNewContactButtonOpensTheForm()
    {
        await using var ctx = new BunitContext();
        Serve(ctx);

        var component = ctx.Render<App.Pages.Contacts>();

        await component.Find("#contact-new").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("dialog"), Is.Not.Null);
            Assert.That(component.Find("dialog").TextContent, Does.Contain("Nový kontakt"));
        }
    }

    [Test]
    public async Task AContactCreatedInTheFormReloadsTheList()
    {
        await using var ctx = new BunitContext();
        var contactsClient = Serve(ctx);

        contactsClient
            .CreateContact(Arg.Any<ContactEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactListItem { ContactId = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = "Nový úřad" }));

        var component = ctx.Render<App.Pages.Contacts>();

        await component.Find("#contact-new").ClickAsync(new MouseEventArgs());
        await component.Find("#contact-create-name").ChangeAsync(new ChangeEventArgs { Value = "Nový úřad" });
        await component.Find(".ec-modal-footer .ec-button-primary").ClickAsync(new MouseEventArgs());

        await contactsClient.Received(2).ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>());
    }

    private static IContactsClient Serve(BunitContext ctx, int total = 0)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var response = new ContactListResponse { Items = [], TotalCount = total };

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));

        ctx.Services.AddSingleton(contactsClient);

        return contactsClient;
    }
}
