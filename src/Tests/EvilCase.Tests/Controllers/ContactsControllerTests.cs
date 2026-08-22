using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Controllers;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Controllers;

public class ContactsControllerTests
{
    [Test]
    public async Task TheRequestReachesTheReaderUntouched()
    {
        var reader = new RecordingContactReader();
        var controller = new ContactsController();
        var request = new ContactListRequest { Search = "úřad" };

        await controller.ListContacts(request, reader, CancellationToken.None);

        Assert.That(reader.Request?.Search, Is.EqualTo("úřad"));
    }

    [Test]
    public async Task TheItemsAreReturnedInTheOrderTheReaderGaveThem()
    {
        var reader = new RecordingContactReader { Items = [Item("Krajský soud ve Vzorově"), Item("Česká advokátní komora")] };
        var controller = new ContactsController();

        var response = await controller.ListContacts(new ContactListRequest(), reader, CancellationToken.None);

        Assert.That(response.Items.Select(item => item.Name), Is.EqualTo(["Krajský soud ve Vzorově", "Česká advokátní komora"]));
    }

    private static ContactListItem Item(string name)
    {
        return new() { Id = Guid.CreateVersion7(), Kind = ContactKind.Authority, Name = name };
    }

    private sealed class RecordingContactReader : IContactReader
    {
        public ContactListRequest? Request { get; private set; }

        public IReadOnlyList<ContactListItem> Items { get; init; } = [];

        public Task<IReadOnlyList<ContactListItem>> List(ContactListRequest request, CancellationToken cancellationToken = default)
        {
            this.Request = request;

            return Task.FromResult(this.Items);
        }
    }
}
