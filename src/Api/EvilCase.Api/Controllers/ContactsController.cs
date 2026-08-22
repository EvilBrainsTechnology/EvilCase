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
    public async Task<ContactListResponse> ListContacts([FromServices] IContactReader contacts, [FromQuery] ContactListRequest request, CancellationToken cancellationToken)
    {
        var items = await contacts.List(request, cancellationToken);

        return new ContactListResponse { Items = items };
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDetail>> GetContact([FromServices] IContactReader contacts, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var contact = await contacts.Detail(id, cancellationToken);

        return contact is null
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found")
            : this.Ok(contact);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> EditContact([FromServices] IContactWriter writer, [FromRoute] Guid id, [FromBody] ContactEditRequest request, CancellationToken cancellationToken)
    {
        var outcome = await writer.Update(id, request, cancellationToken);

        return outcome == ContactUpdateOutcome.NotFound
            ? this.Problem(statusCode: StatusCodes.Status404NotFound, title: "Contact not found")
            : this.NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DeleteContact([FromServices] IContactWriter writer, [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var outcome = await writer.Delete(id, cancellationToken);

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
