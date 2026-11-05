using System.Text;
using MimeKit;
using MimeKit.IO;
using MsgReader.Outlook;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Reads an e-mail for the preview: a name ending in .msg as an Outlook message, any other as a MIME message (eml).
/// </summary>
internal static class EmailFile
{
    private const int MaxBodyLength = 200000;

    private const string UnnamedAttachment = "příloha";

    /// <summary>
    /// The assemblies the readers need, which the app downloads only when the first e-mail opens; the project file lists
    /// the same ones for lazy loading.
    /// </summary>
    public static readonly string[] Assemblies =
    [
        "MimeKitLite.wasm",
        "MsgReader.wasm",
        "RtfPipe.wasm",
        "UtfUnknown.wasm",
        "Microsoft.Maui.Graphics.wasm",
        "Microsoft.IO.RecyclableMemoryStream.wasm",
        "System.Security.Cryptography.Pkcs.wasm",
        "System.Text.Encoding.CodePages.wasm",
    ];

    /// <summary>
    /// The message, or <see langword="null"/> for content its reader cannot read. A message without a text body gets
    /// its HTML body as text from <paramref name="htmlToText"/>. The caller loads <see cref="Assemblies"/> first.
    /// </summary>
    public static async Task<EmailPreview?> ParseOrDefault(string fileName, byte[] content, Func<string, Task<string>> htmlToText)
    {
        // The charsets of older Czech mail, windows-1250 and iso-8859-2 among them, are code pages.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        (EmailPreview Message, string? Html) read;

        try
        {
            read = string.Equals(Path.GetExtension(fileName), ".msg", StringComparison.OrdinalIgnoreCase) ? ReadMsg(content) : ReadEml(content);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Each reader throws its own exceptions for a file it cannot read.
            return null;
        }

        var body = read.Message.Body;

        if (body.Length == 0 && read.Html is { Length: > 0 } html)
            body = await htmlToText(html);

        return read.Message with { Body = body[..Math.Min(body.Length, MaxBodyLength)], Truncated = body.Length > MaxBodyLength };
    }

    private static (EmailPreview Message, string? Html) ReadEml(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var mail = MimeMessage.Load(stream);

        var from = mail.From.Mailboxes.FirstOrDefault() is { } sender ? Address(sender.Name, sender.Address) : "";

        // A missing or unreadable Date header reads as the minimum.
        var date = mail.Date == DateTimeOffset.MinValue ? (DateTimeOffset?)null : mail.Date;

        // Every part but the text and HTML body is an attachment, an inline picture included.
        var attachments = mail.BodyParts
            .Where(static part => part.IsAttachment || part is not TextPart { IsPlain: true } and not TextPart { IsHtml: true })
            .Select(static part => new EmailAttachment(AttachmentName(part), AttachmentSize(part)))
            .ToList();

        var message = new EmailPreview(
            from,
            [.. mail.To.Mailboxes.Select(static mailbox => Address(mailbox.Name, mailbox.Address))],
            [.. mail.Cc.Mailboxes.Select(static mailbox => Address(mailbox.Name, mailbox.Address))],
            mail.Subject ?? "",
            date,
            mail.TextBody ?? "",
            Truncated: false,
            attachments);

        return (message, mail.HtmlBody);
    }

    private static string AttachmentName(MimeEntity part)
    {
        var name = part is MimePart mimePart ? mimePart.FileName : part.ContentDisposition?.FileName ?? part.ContentType.Name;

        return string.IsNullOrEmpty(name) ? UnnamedAttachment : name;
    }

    private static long AttachmentSize(MimeEntity part)
    {
        using var measure = new MeasuringStream();

        if (part is MimePart { Content: { } mimeContent })
            mimeContent.DecodeTo(measure);
        else if (part is MessagePart { Message: { } attached })
            attached.WriteTo(measure);

        return measure.Length;
    }

    private static (EmailPreview Message, string? Html) ReadMsg(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var mail = new Storage.Message(stream);

        var attachments = mail.Attachments.ConvertAll(static attachment => attachment switch
        {
            Storage.Attachment file => new EmailAttachment(Name(file.FileName), file.Data?.Length ?? 0),
            Storage.Message attached => new EmailAttachment(Name(attached.FileName), attached.Size ?? 0),
            _ => new EmailAttachment(UnnamedAttachment, 0),
        });

        var message = new EmailPreview(
            Address(mail.Sender?.DisplayName, mail.Sender?.Email),
            Recipients(mail, RecipientType.To),
            Recipients(mail, RecipientType.Cc),
            mail.Subject ?? "",
            mail.ReceivedOn ?? mail.SentOn ?? mail.CreationTime,
            mail.BodyText ?? "",
            Truncated: false,
            attachments);

        return (message, mail.BodyHtml);

        static string Name(string? name)
        {
            return string.IsNullOrEmpty(name) ? UnnamedAttachment : name;
        }
    }

    // A recipient without a type is an addressee.
    private static List<string> Recipients(Storage.Message mail, RecipientType type)
    {
        return
        [
            .. mail.Recipients
                .Where(recipient => (recipient.Type ?? RecipientType.To) == type)
                .Select(static recipient => Address(recipient.DisplayName, recipient.Email)),
        ];
    }

    private static string Address(string? name, string? address)
    {
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(address) && !string.Equals(name, address, StringComparison.Ordinal))
            return $"{name} <{address}>";

        return !string.IsNullOrEmpty(address) ? address : name ?? "";
    }
}
