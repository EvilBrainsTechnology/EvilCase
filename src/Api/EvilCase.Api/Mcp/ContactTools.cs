using System.ComponentModel;
using System.Diagnostics;
using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class ContactTools
{
    [McpServerTool(Name = "list_contacts", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Lists the contacts (authorities, people, companies), one page at a time, with the total count the filter leaves. take "
            + "(1-100) is required, skip starts at 0. search matches the name and the data box id; kind narrows to one kind.")]
    public static async Task<ContactListResponse> ListContacts(
        IContactReader contacts, [Description("Paging, order and filters.")] ContactListRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        return await contacts.ListContacts(request, token);
    }

    [McpServerTool(Name = "get_contact", ReadOnly = true, OpenWorld = false)]
    [Description("Reads one contact.")]
    public static async Task<ContactDetail> GetContact(IContactReader contacts, [Description("The contact.")] Guid contactId, CancellationToken token)
    {
        return await contacts.GetContactDetail(contactId, token) ?? throw McpProblem.Of(ContactProblems.NotFound);
    }

    [McpServerTool(Name = "create_contact", Destructive = false, OpenWorld = false)]
    [Description(
        "Creates a contact. name is required (at most 256 characters), dataBoxId at most 16, address at most 1024. Returns the "
            + "new contact.")]
    public static async Task<ContactListItem> CreateContact(IContactWriter writer, [Description("The new contact.")] ContactEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        return await writer.CreateContact(request, token);
    }

    [McpServerTool(Name = "update_contact", Destructive = false, OpenWorld = false)]
    [Description(
        "Replaces every field of a contact; read it with get_contact first. name is required (at most 256 characters), dataBoxId "
            + "at most 16, address at most 1024.")]
    public static async Task<string> UpdateContact(
        IContactWriter writer, [Description("The contact.")] Guid contactId, [Description("The contact's new fields.")] ContactEditRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        var outcome = await writer.UpdateContact(contactId, request, token);

        return outcome switch
        {
            ContactUpdateOutcome.Updated => "The contact was updated.",
            ContactUpdateOutcome.NotFound => throw McpProblem.Of(ContactProblems.NotFound),
            _ => throw new UnreachableException(),
        };
    }

    [McpServerTool(Name = "delete_contact", Destructive = true, OpenWorld = false)]
    [Description("Deletes a contact no case or act names; a named one is refused. Cannot be undone.")]
    public static async Task<string> DeleteContact(IContactWriter writer, [Description("The contact.")] Guid contactId, CancellationToken token)
    {
        var outcome = await writer.DeleteContact(contactId, token);

        return outcome switch
        {
            ContactDeleteOutcome.Deleted => "The contact was deleted.",
            ContactDeleteOutcome.NotFound => throw McpProblem.Of(ContactProblems.NotFound),
            ContactDeleteOutcome.Referenced => throw McpProblem.Of("Contact in use", "The contact is named by a case or an act."),
            _ => throw new UnreachableException(),
        };
    }
}
