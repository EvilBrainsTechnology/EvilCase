using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/contacts")]
public class ContactsController(IContactReader contacts) : ControllerBase
{
    [HttpGet("")]
    public async Task<ContactListResponse> ListContacts([FromQuery] ContactListRequest request, CancellationToken cancellationToken)
    {
        var items = await contacts.List(request, cancellationToken);

        return new ContactListResponse { Items = items };
    }
}
