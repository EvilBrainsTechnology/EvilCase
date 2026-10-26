using System.Text;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DataMessageZfoTests
{
    [Test]
    public void AMessageReadsItsEnvelopeAndAttachments()
    {
        byte[] decision = [1, 2, 3];
        var notice = Encoding.UTF8.GetBytes("Poučení o odvolání.");

        var message = DataMessageZfo.ParseOrDefault(SyntheticZfo.Message(("Rozhodnutí.pdf", decision), ("Poučení.txt", notice)))!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Kind, Is.EqualTo(DataMessageKind.Message));
            Assert.That(message.Subject, Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(message.Sender, Is.EqualTo(new DataMessageParty("Testovací úřad", "tst0001", "Náměstí 1, 100 00 Praha 1")));
            Assert.That(message.Recipient, Is.EqualTo(new DataMessageParty("Jan Testovací", "tst0002", "Ulice 2, 602 00 Brno")));
            Assert.That(message.SenderRefNumber, Is.EqualTo("ČJ-1/2026"));
            Assert.That(message.SenderIdent, Is.EqualTo("SZ-7/2026"));
            Assert.That(message.RecipientRefNumber, Is.Empty);
            Assert.That(message.RecipientIdent, Is.Empty);
            Assert.That(message.MessageId, Is.EqualTo("1234567"));
            Assert.That(message.Delivered, Is.EqualTo(new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.FromHours(1))));
            Assert.That(message.Accepted, Is.EqualTo(new DateTimeOffset(2026, 3, 3, 10, 20, 0, TimeSpan.FromHours(1))));
            Assert.That(message.Attachments.Select(static attachment => attachment.Name), Is.EqualTo(["Rozhodnutí.pdf", "Poučení.txt"]));
            Assert.That(message.Attachments.Select(static attachment => attachment.Content), Is.EqualTo([decision, notice]));
            Assert.That(message.Events, Is.Empty);
        }
    }

    [TestCase("http://isds.czechpoint.cz/v20")]
    [TestCase("http://isds.czechpoint.cz/v24")]
    public void AnyIsdsNamespaceVersionReads(string isdsNamespace)
    {
        var message = DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(SyntheticZfo.MessageXml(isdsNamespace, [("a.txt", [1])])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message?.Subject, Is.EqualTo("Rozhodnutí o přestupku"), "elements match by local name");
            Assert.That(message?.Attachments, Has.Count.EqualTo(1), "elements match by local name");
        }
    }

    [Test]
    public void AReceiptReadsItsEventsWithoutTheEventCodes()
    {
        var receipt = DataMessageZfo.ParseOrDefault(SyntheticZfo.Receipt())!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receipt.Kind, Is.EqualTo(DataMessageKind.Receipt));
            Assert.That(receipt.Attachments, Is.Empty);
            Assert.That(
                receipt.Events.Select(static item => item.Description),
                Is.EqualTo(["Zpráva byla podána.", "Zpráva byla dodána do schránky.", "Zpráva byla doručena přihlášením oprávněné osoby."]));
            Assert.That(receipt.Events[0].Time, Is.EqualTo(new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.FromHours(1))));
        }
    }

    [Test]
    public void AnXmlContentAttachmentCarriesItsXmlAsText()
    {
        var xml = SyntheticZfo.MessageXml(SyntheticZfo.Namespace, []).Replace(
            "<p:dmFiles></p:dmFiles>",
            """<p:dmFiles><p:dmFile dmMimeType="application/xml" dmFileMetaType="main" dmFileDescr="Formulář.xml"><p:dmXMLContent><f:formular xmlns:f="urn:test:formular"><f:pole>hodnota</f:pole></f:formular></p:dmXMLContent></p:dmFile></p:dmFiles>""",
            StringComparison.Ordinal);

        var attachment = DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(xml))!.Attachments.Single();
        var text = Encoding.UTF8.GetString(attachment.Content);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(attachment.Name, Is.EqualTo("Formulář.xml"));
            Assert.That(text, Does.StartWith("<f:formular"));
            Assert.That(text, Does.Contain("<f:pole>hodnota</f:pole>"));
        }
    }

    [TestCase(@"..\..\Tajné.pdf", "Tajné.pdf")]
    [TestCase("složka/a:b?.pdf", "a_b_.pdf")]
    [TestCase(" . ", "příloha")]
    [TestCase("<skript>.txt", "_skript_.txt")]
    [TestCase("faktura\u202Efdp.exe", "faktura_fdp.exe")]
    [TestCase("a\u2066b\u200F.pdf", "a_b_.pdf")]
    public void AnAttachmentNameKeepsNothingAFileNameCannotCarry(string description, string name)
    {
        var message = DataMessageZfo.ParseOrDefault(SyntheticZfo.Message((description, [1])));

        Assert.That(message?.Attachments.Single().Name, Is.EqualTo(name));
    }

    [TestCase("")]
    [TestCase("abc")]
    [TestCase("<form/>")]
    public void AnythingButACmsSignedDataIsNotRead(string content)
    {
        Assert.That(DataMessageZfo.ParseOrDefault(Encoding.UTF8.GetBytes(content)), Is.Null);
    }

    [Test]
    public void ALengthBeyondTheContentIsNotRead()
    {
        byte[] zfo = [0x30, 0x84, 0x7F, 0xFF, 0xFF, 0xFF, 0x06, 0x01, 0x00];

        Assert.That(DataMessageZfo.ParseOrDefault(zfo), Is.Null);
    }

    [Test]
    public void ADeeplyNestedStructureIsNotRead()
    {
        const int depth = 100_000;
        byte[] head = [0x30, 0x80, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x02, 0xA0, 0x80, 0x30, 0x80, 0x02, 0x01, 0x01, 0x31, 0x80];
        var zfo = new List<byte>(head);

        for (var level = 0; level < depth; level++)
            zfo.AddRange([0x30, 0x80]);

        for (var level = 0; level < depth + 4; level++)
            zfo.AddRange([0x00, 0x00]);

        Assert.That(DataMessageZfo.ParseOrDefault([.. zfo]), Is.Null);
    }

    [Test]
    public void ADeeplyNestedXmlIsNotRead()
    {
        var nested = string.Concat(Enumerable.Repeat("<a>", 100_000)) + string.Concat(Enumerable.Repeat("</a>", 100_000));
        var xml = SyntheticZfo.MessageXml(SyntheticZfo.Namespace, []).Replace("Rozhodnutí o přestupku", nested, StringComparison.Ordinal);

        Assert.That(DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(xml)), Is.Null, "a nesting deeper than any message is refused before the load");
    }

    [TestCase("Toto není XML")]
    [TestCase("<?xml version=\"1.0\"?><formular xmlns=\"urn:test:formular\"><pole>hodnota</pole></formular>")]
    public void ASignedContentThatIsNoIsdsMessageIsNotRead(string content)
    {
        Assert.That(DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(content)), Is.Null, "a Software602 form or other content falls back to the download");
    }

    [Test]
    public void ADocumentTypeIsRefused()
    {
        var xml = SyntheticZfo.MessageXml(SyntheticZfo.Namespace, [("a.txt", [1])])
            .Replace("<q:MessageDownloadResponse", "<!DOCTYPE zprava [<!ENTITY a \"b\">]><q:MessageDownloadResponse", StringComparison.Ordinal);

        Assert.That(DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(xml)), Is.Null, "no DTD is processed");
    }

    [Test]
    public void AnAttachmentThatIsNoBase64LeavesTheMessageUnread()
    {
        var xml = SyntheticZfo.MessageXml(SyntheticZfo.Namespace, [("a.txt", [1])]).Replace("AQ==", "@@@", StringComparison.Ordinal);

        Assert.That(DataMessageZfo.ParseOrDefault(SyntheticZfo.Sign(xml)), Is.Null);
    }
}
