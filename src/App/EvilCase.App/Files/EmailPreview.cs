namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A message as the preview shows it: the headers, the body as plain text, and the attachments by name.
/// </summary>
internal sealed record EmailPreview(
    string From,
    IReadOnlyList<string> To,
    IReadOnlyList<string> Cc,
    string Subject,
    DateTimeOffset? Date,
    string Body,
    bool Truncated,
    IReadOnlyList<EmailAttachment> Attachments);
