using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.App.Clipboard;
using EvilBrains.EvilCase.App.Components.Lists;
using EvilBrains.EvilCase.App.Pages;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class ContactRenderTests
{
    [Test]
    public void TheContactRenders()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var contactId = Guid.CreateVersion7();

        var contactsClient = Substitute.For<IContactsClient>();
        contactsClient
            .GetContact(contactId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ContactDetail { ContactId = contactId, Name = "Městský úřad", Kind = ContactKind.Authority }));

        var casesClient = Substitute.For<ICasesClient>();
        casesClient
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CaseListResponse { Items = [], TotalCount = 0 }));

        var actsClient = Substitute.For<IActsClient>();
        actsClient
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ActListResponse { Items = [], TotalCount = 0 }));

        ctx.Services.AddSingleton<IClipboardWriter>(new StubClipboardWriter());
        ctx.Services.AddSingleton(contactsClient);
        ctx.Services.AddSingleton(casesClient);
        ctx.Services.AddSingleton(actsClient);

        var component = ctx.Render<Contact>(parameters => parameters.Add(static page => page.ContactId, contactId));

        Assert.That(component.FindComponents<ActList>(), Has.Count.EqualTo(1));
    }
}
