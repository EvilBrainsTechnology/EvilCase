using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Components.Lists;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactsRenderTests
{
    [Test]
    public void TheListRenders()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .ListContacts(Arg.Any<ContactListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactListResponse { Items = [], TotalCount = 0 }));

        ctx.Services.AddSingleton(contactsClient);

        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/contacts?q=urad");

        var component = ctx.Render<App.Pages.Contacts>();

        Assert.That(component.FindComponents<ContactList>(), Has.Count.EqualTo(1));
    }
}
