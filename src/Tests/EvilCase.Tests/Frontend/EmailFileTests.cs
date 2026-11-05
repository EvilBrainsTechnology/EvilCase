using System.Text;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EmailFileTests
{
    private const string Headers = """
        From: =?UTF-8?Q?Jan_Testovac=C3=AD?= <jan@example.test>
        To: Spis: Eva Ukázková <eva@example.test>, petr@example.test;, Úřad <urad@example.test>
        Cc: "kopie@example.test" <kopie@example.test>, Prázdná skupina: ;
        Subject: =?UTF-8?Q?Rozhodnut=C3=AD_o_p=C5=99estupku?=
        Date: Mon, 2 Mar 2026 10:15:00 +0100
        MIME-Version: 1.0

        """;

    [Test]
    public async Task AnEmlReadsItsHeadersWithTheGroupsFlattened()
    {
        var message = await Read("zprava.eml", Headers + "Content-Type: text/plain; charset=utf-8\n\nDobrý den.\n");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.From, Is.EqualTo("Jan Testovací <jan@example.test>"));
            Assert.That(message.To, Is.EqualTo(["Eva Ukázková <eva@example.test>", "petr@example.test", "Úřad <urad@example.test>"]), "a group is listed by its members");
            Assert.That(message.Cc, Is.EqualTo(["kopie@example.test"]), "a name equal to the address shows once, an empty group not at all");
            Assert.That(message.Subject, Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(message.Date, Is.EqualTo(new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.Zero)));
            Assert.That(message.Body, Is.EqualTo("Dobrý den.\n"));
            Assert.That(message.Attachments, Is.Empty);
        }
    }

    [TestCase("windows-1250", new byte[] { 0x9E, 0x6C, 0x75, 0x9D, 0x6F, 0x75, 0xE8, 0x6B, 0xFD })]
    [TestCase("iso-8859-2", new byte[] { 0xBE, 0x6C, 0x75, 0xBB, 0x6F, 0x75, 0xE8, 0x6B, 0xFD })]
    public async Task ABodyInALegacyCzechCharsetReads(string charset, byte[] body)
    {
        byte[] eml = [.. Encoding.ASCII.GetBytes($"Content-Type: text/plain; charset={charset}\n\n"), .. body];

        var message = await EmailFile.ParseOrDefault("zprava.eml", eml, Task.FromResult);

        Assert.That(message?.Body, Is.EqualTo("žluťoučký"));
    }

    [Test]
    public async Task AnEmlWithoutHeadersShowsThemEmpty()
    {
        var message = await Read("zprava.eml", "\nText bez hlaviček.\n");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.From, Is.Empty);
            Assert.That(message.To, Is.Empty);
            Assert.That(message.Subject, Is.Empty);
            Assert.That(message.Date, Is.Null);
        }
    }

    [Test]
    public async Task AnHtmlBodyAloneBecomesTheText()
    {
        var converted = new List<string>();
        const string eml = $"""
            {Headers}Content-Type: text/html; charset=utf-8

            <p>Dobrý den.</p>
            """;

        var message = await EmailFile.ParseOrDefault("zprava.eml", Encoding.UTF8.GetBytes(eml), async html => await Convert(converted, html));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(converted, Is.EqualTo(["<p>Dobrý den.</p>"]));
            Assert.That(message?.Body, Is.EqualTo("text of <p>Dobrý den.</p>"));
        }
    }

    [Test]
    public async Task ATextBodyBeatsTheHtmlOne()
    {
        var converted = new List<string>();
        const string eml = $"""
            {Headers}Content-Type: multipart/alternative; boundary="b"

            --b
            Content-Type: text/plain; charset=utf-8

            Prostý text.
            --b
            Content-Type: text/html; charset=utf-8

            <p>HTML</p>
            --b--
            """;

        var message = await EmailFile.ParseOrDefault("zprava.eml", Encoding.UTF8.GetBytes(eml), async html => await Convert(converted, html));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message?.Body, Is.EqualTo("Prostý text."));
            Assert.That(converted, Is.Empty, "the HTML body is converted only when there is no text body");
        }
    }

    [Test]
    public async Task EveryPartButTheBodiesIsAnAttachmentOfItsDecodedSize()
    {
        const string eml = $"""
            {Headers}Content-Type: multipart/mixed; boundary="m"

            --m
            Content-Type: multipart/related; boundary="r"

            --r
            Content-Type: text/html; charset=utf-8

            <p><img src="cid:logo"></p>
            --r
            Content-Type: image/png
            Content-ID: <logo>
            Content-Transfer-Encoding: base64

            AQID
            --r--
            --m
            Content-Type: application/pdf; name="=?UTF-8?Q?Rozhodnut=C3=AD.pdf?="
            Content-Disposition: attachment
            Content-Transfer-Encoding: base64

            AQIDBAU=
            --m
            Content-Type: text/plain
            Content-Disposition: attachment; filename="pozn.txt"

            abcd
            --m
            Content-Type: message/rfc822

            Subject: Vnořená

            Text.
            --m--
            """;

        var message = await Read("zprava.eml", eml);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Attachments.Select(static attachment => attachment.Name), Is.EqualTo(["příloha", "Rozhodnutí.pdf", "pozn.txt", "příloha"]), "a part without a name is a příloha");
            Assert.That(message.Attachments.Select(static attachment => attachment.Size).Take(2), Is.EqualTo([3, 5]), "the size is that of the decoded content");
        }
    }

    [TestCase(200000, false)]
    [TestCase(200001, true)]
    public async Task ABodyOverTheLimitIsCutAndMarked(int length, bool truncated)
    {
        var message = await Read("zprava.eml", Headers + "\n" + new string('a', length));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message.Body, Has.Length.EqualTo(Math.Min(length, 200000)));
            Assert.That(message.Truncated, Is.EqualTo(truncated));
        }
    }

    [Test]
    public async Task AnMsgReadsItsHeadersBodyAndAttachments()
    {
        var msg = SyntheticMsg.Message(
            "Rozhodnutí o přestupku",
            ("Jan Testovací", "jan@example.test"),
            [("Eva Ukázková", "eva@example.test", 1), ("petr@example.test", "petr@example.test", 1), ("Úřad", "urad@example.test", 2)],
            "Dobrý den.",
            html: null,
            new DateTime(2026, 3, 2, 9, 15, 0, DateTimeKind.Utc),
            [("Rozhodnutí.pdf", [1, 2, 3, 4, 5])]);

        var message = await EmailFile.ParseOrDefault("Zpráva.MSG", msg, Task.FromResult);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message?.From, Is.EqualTo("Jan Testovací <jan@example.test>"));
            Assert.That(message?.To, Is.EqualTo(["Eva Ukázková <eva@example.test>", "petr@example.test"]));
            Assert.That(message?.Cc, Is.EqualTo(["Úřad <urad@example.test>"]));
            Assert.That(message?.Subject, Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(message?.Date, Is.EqualTo(new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.Zero)));
            Assert.That(message?.Body.TrimEnd(), Is.EqualTo("Dobrý den."));
            Assert.That(message?.Attachments, Is.EqualTo([new EmailAttachment("Rozhodnutí.pdf", 5)]));
        }
    }

    [TestCase("zprava.msg")]
    [TestCase("zprava.eml")]
    public async Task ContentItsReaderCannotReadIsNoMessage(string fileName)
    {
        var message = await EmailFile.ParseOrDefault(fileName, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0], Task.FromResult);

        Assert.That(message, Is.Null);
    }

    private static async Task<EmailPreview> Read(string fileName, string content)
    {
        var message = await EmailFile.ParseOrDefault(fileName, Encoding.UTF8.GetBytes(content), Task.FromResult);

        return message ?? throw new AssertionException("The content is a message.");
    }

    private static Task<string> Convert(List<string> converted, string html)
    {
        converted.Add(html);

        return Task.FromResult($"text of {html}");
    }
}
