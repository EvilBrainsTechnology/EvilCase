using System.Buffers.Binary;
using System.Text;
using OpenMcdf;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Builds an Outlook message (msg) as the compound file MS-OXMSG describes, with only the properties the preview reads.
/// </summary>
internal static class SyntheticMsg
{
    private const ushort Unicode = 0x001F;

    private const ushort Binary = 0x0102;

    private const ushort Integer = 0x0003;

    private const ushort Time = 0x0040;

    public static byte[] Message(
        string subject,
        (string Name, string Address) sender,
        IReadOnlyList<(string Name, string Address, int Type)> recipients,
        string? body,
        string? html,
        DateTime delivered,
        IReadOnlyList<(string Name, byte[] Content)> attachments)
    {
        using var stream = new MemoryStream();

        using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
        {
            var properties = new List<byte[]>();

            Text(root, properties, 0x001A, "IPM.Note");
            Text(root, properties, 0x0037, subject);
            Text(root, properties, 0x0C1A, sender.Name);
            Text(root, properties, 0x0C1E, "SMTP");
            Text(root, properties, 0x0C1F, sender.Address);
            Text(root, properties, 0x5D01, sender.Address);

            if (body is not null)
                Text(root, properties, 0x1000, body);

            if (html is not null)
                Bytes(root, properties, 0x1013, Encoding.UTF8.GetBytes(html));

            properties.Add(Entry(0x0E06, Time, BitConverter.GetBytes(delivered.ToFileTimeUtc())));

            for (var index = 0; index < recipients.Count; index++)
                Recipient(root.CreateStorage($"__recip_version1.0_#{index:X8}"), recipients[index]);

            for (var index = 0; index < attachments.Count; index++)
                Attachment(root.CreateStorage($"__attach_version1.0_#{index:X8}"), attachments[index]);

            root.CreateStorage("__nameid_version1.0");

            var header = new byte[32];
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8), recipients.Count);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12), attachments.Count);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), recipients.Count);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(20), attachments.Count);
            WriteProperties(root, header, properties);
        }

        return stream.ToArray();
    }

    private static void Recipient(Storage storage, (string Name, string Address, int Type) recipient)
    {
        var properties = new List<byte[]>();

        Text(storage, properties, 0x3001, recipient.Name);
        Text(storage, properties, 0x3002, "SMTP");
        Text(storage, properties, 0x3003, recipient.Address);
        Text(storage, properties, 0x39FE, recipient.Address);
        properties.Add(Entry(0x0C15, Integer, BitConverter.GetBytes(recipient.Type)));
        WriteProperties(storage, new byte[8], properties);
    }

    private static void Attachment(Storage storage, (string Name, byte[] Content) attachment)
    {
        var properties = new List<byte[]>();

        Text(storage, properties, 0x3707, attachment.Name);
        Text(storage, properties, 0x3704, attachment.Name);
        Bytes(storage, properties, 0x3701, attachment.Content);
        properties.Add(Entry(0x3705, Integer, BitConverter.GetBytes(1)));
        properties.Add(Entry(0x0E20, Integer, BitConverter.GetBytes(attachment.Content.Length)));
        WriteProperties(storage, new byte[8], properties);
    }

    private static void Text(Storage storage, List<byte[]> properties, ushort id, string value)
    {
        Bytes(storage, properties, id, Encoding.Unicode.GetBytes(value), Unicode);
    }

    private static void Bytes(Storage storage, List<byte[]> properties, ushort id, byte[] value, ushort type = Binary)
    {
        using (var stream = storage.CreateStream($"__substg1.0_{id:X4}{type:X4}"))
            stream.Write(value);

        properties.Add(Entry(id, type, BitConverter.GetBytes((long)value.Length)));
    }

    private static byte[] Entry(ushort id, ushort type, byte[] value)
    {
        var entry = new byte[16];

        BinaryPrimitives.WriteUInt32LittleEndian(entry, ((uint)id << 16) | type);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.AsSpan(4), 0x6);
        value.CopyTo(entry.AsSpan(8));

        return entry;
    }

    private static void WriteProperties(Storage storage, byte[] header, List<byte[]> properties)
    {
        using var stream = storage.CreateStream("__properties_version1.0");

        stream.Write(header);

        foreach (var property in properties)
            stream.Write(property);
    }
}
