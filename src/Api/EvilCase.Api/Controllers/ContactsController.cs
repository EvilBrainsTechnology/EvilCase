using System.Diagnostics;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    [HttpGet("")]
    public async Task<ContactListResponse> ListContacts([FromServices] IContactReader contacts, [FromQuery] ContactListRequest request, CancellationToken token)
    {
        var items = await contacts.ListContacts(request, token);

        return new ContactListResponse { Items = items };
    }

    [HttpPost("")]
    public Task<ContactListItem> CreateContact([FromServices] IContactWriter writer, [FromBody] ContactEditRequest request, CancellationToken token)
    {
        return writer.CreateContact(request, token);
    }

    [HttpGet("{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDetail>> GetContact([FromServices] IContactReader contacts, [FromRoute] Guid contactId, CancellationToken token)
    {
        var contact = await contacts.GetContactDetail(contactId, token);

        return contact is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found")
            : this.Ok(contact);
    }

    [HttpPut("{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditContact([FromServices] IContactWriter writer, [FromRoute] Guid contactId, [FromBody] ContactEditRequest request, CancellationToken token)
    {
        var outcome = await writer.UpdateContact(contactId, request, token);

        return outcome == ContactUpdateOutcome.NotFound
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found")
            : this.NoContent();
    }

    [HttpDelete("{contactId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteContact([FromServices] IContactWriter writer, [FromRoute] Guid contactId, CancellationToken token)
    {
        var outcome = await writer.DeleteContact(contactId, token);

        return outcome switch
        {
            ContactDeleteOutcome.Deleted => this.NoContent(),
            ContactDeleteOutcome.NotFound => this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found"),
            ContactDeleteOutcome.DefaultContact => this.Problem(
                detail: "The default contact cannot be deleted.", statusCode: StatusCodes.Status409Conflict, title: "Contact in use"),
            ContactDeleteOutcome.Referenced => this.Problem(
                detail: "The contact is referenced by a case or an act.", statusCode: StatusCodes.Status409Conflict, title: "Contact in use"),
            _ => throw new UnreachableException(),
        };
    }
}
