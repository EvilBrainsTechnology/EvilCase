using System.Formats.Asn1;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A ZFO as ISDS issues it, signed in the test: SignedCms has no reference assembly here, so the CMS is written with
/// AsnWriter.
/// </summary>
internal static class SyntheticZfo
{
    public const string Namespace = "http://isds.czechpoint.cz/v20";

    private const string Envelope = """
        <p:dmID>1234567</p:dmID>
        <p:dbIDSender>tst0001</p:dbIDSender>
        <p:dmSender>Testovací úřad</p:dmSender>
        <p:dmSenderAddress>Náměstí 1, 100 00 Praha 1</p:dmSenderAddress>
        <p:dmRecipient>Jan Testovací</p:dmRecipient>
        <p:dmRecipientAddress>Ulice 2, 602 00 Brno</p:dmRecipientAddress>
        <p:dbIDRecipient>tst0002</p:dbIDRecipient>
        <p:dmAnnotation>Rozhodnutí o přestupku</p:dmAnnotation>
        <p:dmRecipientRefNumber></p:dmRecipientRefNumber>
        <p:dmSenderRefNumber>ČJ-1/2026</p:dmSenderRefNumber>
        <p:dmRecipientIdent/>
        <p:dmSenderIdent>SZ-7/2026</p:dmSenderIdent>
        """;

    private const string Sha256 = "2.16.840.1.101.3.4.2.1";

    public static byte[] Message(params (string Name, byte[] Content)[] files)
    {
        return Sign(MessageXml(Namespace, files));
    }

    public static byte[] Receipt()
    {
        return Sign(ReceiptXml(Namespace));
    }

    public static string MessageXml(string isdsNamespace, IEnumerable<(string Name, byte[] Content)> files)
    {
        var dmFiles = string.Concat(files.Select(static file =>
            $"""<p:dmFile dmMimeType="application/octet-stream" dmFileMetaType="enclosure" dmFileDescr="{SecurityElement.Escape(file.Name)}"><p:dmEncodedContent>{Convert.ToBase64String(file.Content)}</p:dmEncodedContent></p:dmFile>"""));

        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <q:MessageDownloadResponse xmlns:q="{isdsNamespace}/message" xmlns:p="{isdsNamespace}">
              <q:dmReturnedMessage>
                <p:dmDm>{Envelope}<p:dmFiles>{dmFiles}</p:dmFiles></p:dmDm>
                <q:dmHash algorithm="SHA-256">AAAA</q:dmHash>
                <q:dmDeliveryTime>2026-03-02T09:15:00.000+01:00</q:dmDeliveryTime>
                <q:dmAcceptanceTime>2026-03-03T10:20:00.000+01:00</q:dmAcceptanceTime>
                <q:dmMessageStatus>6</q:dmMessageStatus>
              </q:dmReturnedMessage>
            </q:MessageDownloadResponse>
            """;
    }

    public static string ReceiptXml(string isdsNamespace)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <q:GetDeliveryInfoResponse xmlns:q="{isdsNamespace}/info" xmlns:p="{isdsNamespace}">
              <q:dmDelivery>
                <p:dmDm>{Envelope}</p:dmDm>
                <q:dmDeliveryTime>2026-03-02T09:15:00.000+01:00</q:dmDeliveryTime>
                <q:dmAcceptanceTime>2026-03-03T10:20:00.000+01:00</q:dmAcceptanceTime>
                <q:dmEvents>
                  <q:dmEvent>
                    <q:dmEventTime>2026-03-02T09:15:00.000+01:00</q:dmEventTime>
                    <q:dmEventDescr>EV0: Zpráva byla podána.</q:dmEventDescr>
                  </q:dmEvent>
                  <q:dmEvent>
                    <q:dmEventTime>2026-03-02T09:15:01.000+01:00</q:dmEventTime>
                    <q:dmEventDescr>EV5: Zpráva byla dodána do schránky.</q:dmEventDescr>
                  </q:dmEvent>
                  <q:dmEvent>
                    <q:dmEventTime>2026-03-03T10:20:00.000+01:00</q:dmEventTime>
                    <q:dmEventDescr>EV4: Zpráva byla doručena přihlášením oprávněné osoby.</q:dmEventDescr>
                  </q:dmEvent>
                </q:dmEvents>
              </q:dmDelivery>
            </q:GetDeliveryInfoResponse>
            """;
    }

    public static byte[] Sign(string xml)
    {
        var content = Encoding.UTF8.GetBytes(xml);

        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Testovací podepisující, O=Testovací úřad, C=CZ", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.Create(
            request.SubjectName,
            X509SignatureGenerator.CreateForRSA(key, RSASignaturePadding.Pkcs1),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            [1, 2, 3, 4]);
        var signature = key.SignData(content, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var explicitZero = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);
        var writer = new AsnWriter(AsnEncodingRules.DER);

        using (writer.PushSequence()) // ContentInfo
        {
            writer.WriteObjectIdentifier("1.2.840.113549.1.7.2"); // id-signedData

            using (writer.PushSequence(explicitZero))
            using (writer.PushSequence()) // SignedData
            {
                writer.WriteInteger(1);

                using (writer.PushSetOf())
                    WriteAlgorithm(writer, Sha256);

                using (writer.PushSequence()) // EncapsulatedContentInfo
                {
                    writer.WriteObjectIdentifier("1.2.840.113549.1.7.1");

                    using (writer.PushSequence(explicitZero))
                        writer.WriteOctetString(content);
                }

                using (writer.PushSetOf(explicitZero)) // certificates
                    writer.WriteEncodedValue(certificate.RawData);

                using (writer.PushSetOf()) // SignerInfos
                using (writer.PushSequence())
                {
                    writer.WriteInteger(1);

                    using (writer.PushSequence())
                    {
                        writer.WriteEncodedValue(certificate.IssuerName.RawData);
                        writer.WriteInteger([1, 2, 3, 4]);
                    }

                    WriteAlgorithm(writer, Sha256);
                    WriteAlgorithm(writer, "1.2.840.113549.1.1.1"); // rsaEncryption
                    writer.WriteOctetString(signature);
                }
            }
        }

        return writer.Encode();
    }

    private static void WriteAlgorithm(AsnWriter writer, string oid)
    {
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(oid);
            writer.WriteNull();
        }
    }
}
