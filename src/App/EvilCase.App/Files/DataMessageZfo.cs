using System.Formats.Asn1;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Reads an ISDS data message or delivery receipt from its ZFO: a CMS SignedData whose content is the ISDS XML.
/// The signature is not verified. Elements match by local name, so every ISDS namespace version reads.
/// </summary>
internal static partial class DataMessageZfo
{
    /// <summary>
    /// The largest ZFO the preview reads: a standard message of 20 MB of attachments, in base64.
    /// </summary>
    public const long MaxBytes = 30 * 1024 * 1024;

    private const string SignedDataOid = "1.2.840.113549.1.7.2";

    // Deeper than any ISDS message; loading XML slows quadratically with its nesting.
    private const int MaxXmlDepth = 100;

    private const string InvalidNameCharacters = "<>:\"|?*";

    private static readonly Asn1Tag ExplicitZero = new(TagClass.ContextSpecific, 0, isConstructed: true);

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    /// <summary>
    /// The message or receipt, or <see langword="null"/> for anything else, a Software602 form included.
    /// </summary>
    public static DataMessage? ParseOrDefault(byte[] zfo)
    {
        try
        {
            return SignedContent(zfo) is { } xml ? Read(xml) : null;
        }
        catch (Exception exception) when (exception is AsnContentException or XmlException or FormatException)
        {
            return null;
        }
    }

    private static byte[]? SignedContent(byte[] zfo)
    {
        var contentInfo = new AsnReader(zfo, AsnEncodingRules.BER).ReadSequence();
        if (!string.Equals(contentInfo.ReadObjectIdentifier(), SignedDataOid, StringComparison.Ordinal))
            return null;

        var signedData = contentInfo.ReadSequence(ExplicitZero).ReadSequence();
        signedData.ReadEncodedValue(); // version
        signedData.ReadEncodedValue(); // digestAlgorithms
        var encapsulated = signedData.ReadSequence();
        encapsulated.ReadObjectIdentifier();

        // A detached signature carries no content.
        return encapsulated.HasData ? encapsulated.ReadSequence(ExplicitZero).ReadOctetString() : null;
    }

    private static DataMessage? Read(byte[] xml)
    {
        using (var scan = XmlReader.Create(new MemoryStream(xml, writable: false), XmlSettings))
        {
            while (scan.Read())
            {
                if (scan.Depth > MaxXmlDepth)
                    return null;
            }
        }

        using var stream = new MemoryStream(xml, writable: false);
        using var reader = XmlReader.Create(stream, XmlSettings);
        var root = XDocument.Load(reader).Root!;
        var envelope = root.DescendantsAndSelf().FirstOrDefault(static element => element.Name.LocalName is "dmReturnedMessage" or "dmDelivery");
        if (envelope is null || Child(envelope, "dmDm") is not { } dm)
            return null;

        var receipt = string.Equals(envelope.Name.LocalName, "dmDelivery", StringComparison.Ordinal);

        return new DataMessage(
            receipt ? DataMessageKind.Receipt : DataMessageKind.Message,
            Text(dm, "dmAnnotation"),
            new DataMessageParty(Text(dm, "dmSender"), Text(dm, "dbIDSender"), Text(dm, "dmSenderAddress")),
            new DataMessageParty(Text(dm, "dmRecipient"), Text(dm, "dbIDRecipient"), Text(dm, "dmRecipientAddress")),
            Text(dm, "dmSenderRefNumber"),
            Text(dm, "dmSenderIdent"),
            Text(dm, "dmRecipientRefNumber"),
            Text(dm, "dmRecipientIdent"),
            Text(dm, "dmID"),
            Moment(envelope, "dmDeliveryTime"),
            Moment(envelope, "dmAcceptanceTime"),
            receipt ? [] : ReadAttachments(dm),
            receipt ? ReadEvents(envelope) : []);
    }

    private static List<DataMessageAttachment> ReadAttachments(XElement dm)
    {
        var attachments = new List<DataMessageAttachment>();
        foreach (var file in Children(dm, "dmFiles").SelectMany(static files => Children(files, "dmFile")))
        {
            // ISDS carries the description as an attribute; an element of the name reads too.
            var description = Attribute(file, "dmFileDescr") ?? Text(file, "dmFileDescr");
            var name = SafeFileName(description);
            if (Child(file, "dmEncodedContent") is { } encoded)
                attachments.Add(new DataMessageAttachment(name, Convert.FromBase64String(encoded.Value)));
            else if (Child(file, "dmXMLContent")?.Elements().FirstOrDefault() is { } content)
                attachments.Add(new DataMessageAttachment(name, Encoding.UTF8.GetBytes(content.ToString())));
        }

        return attachments;
    }

    private static List<DataMessageEvent> ReadEvents(XElement delivery)
    {
        return
        [
            .. Children(delivery, "dmEvents")
                .SelectMany(static events => Children(events, "dmEvent"))
                .Select(static item => new DataMessageEvent(Moment(item, "dmEventTime"), EventCode.Replace(Text(item, "dmEventDescr"), ""))),
        ];
    }

    // The name the attachment is shown and saved under: its last path segment without what a file name cannot carry.
    private static string SafeFileName(string description)
    {
        var lastSegment = description[(description.LastIndexOfAny(['/', '\\']) + 1)..];
        var safe = new string([.. lastSegment.Select(static character => IsInvalidInName(character) ? '_' : character)]).Trim(' ', '.');

        return safe.Length == 0 ? "příloha" : safe;
    }

    private static bool IsInvalidInName(char character)
    {
        // A format character such as a right-to-left override would disguise the extension.
        return char.IsControl(character)
            || char.GetUnicodeCategory(character) == UnicodeCategory.Format
            || InvalidNameCharacters.Contains(character, StringComparison.Ordinal);
    }

    private static IEnumerable<XElement> Children(XElement parent, string localName)
    {
        return parent.Elements().Where(element => string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal));
    }

    private static string? Attribute(XElement element, string localName)
    {
        return element.Attributes().FirstOrDefault(attribute => string.Equals(attribute.Name.LocalName, localName, StringComparison.Ordinal))?.Value;
    }

    private static XElement? Child(XElement parent, string localName)
    {
        return Children(parent, localName).FirstOrDefault();
    }

    private static string Text(XElement parent, string localName)
    {
        return Child(parent, localName)?.Value.Trim() ?? "";
    }

    private static DateTimeOffset? Moment(XElement parent, string localName)
    {
        return DateTimeOffset.TryParse(Text(parent, localName), CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment) ? moment : null;
    }

    [GeneratedRegex(@"^EV\d+:\s*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EventCode { get; }
}
