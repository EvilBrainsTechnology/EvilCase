namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// An ISDS data message or delivery receipt as the preview shows it.
/// </summary>
internal sealed record DataMessagePreview(
    DataMessageKind Kind,
    string Subject,
    DataMessageParty Sender,
    DataMessageParty Recipient,
    string SenderRefNumber,
    string SenderIdent,
    string RecipientRefNumber,
    string RecipientIdent,
    string MessageId,
    DateTimeOffset? Delivered,
    DateTimeOffset? Accepted,
    IReadOnlyList<DataMessageAttachment> Attachments,
    IReadOnlyList<DataMessageEvent> Events);
