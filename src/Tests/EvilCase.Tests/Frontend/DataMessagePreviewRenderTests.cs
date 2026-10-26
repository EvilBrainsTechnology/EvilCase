using System.Text;
using Bunit;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DataMessagePreviewRenderTests
{
    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private static readonly byte[] TextBytes = Encoding.UTF8.GetBytes("Poučení o odvolání.");

    [Test]
    public void AMessageShowsItsEnvelopeAndAttachmentsInTheReflowingCard()
    {
        using var ctx = NewContext(out var previews, out _);

        var component = Render(ctx, DefaultMessage());
        var definitions = component.FindAll(".ec-preview-data-message dd");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-data-message-subject").TextContent, Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(component.Find(".ec-preview-data-message .ec-badge").TextContent.Trim(), Is.EqualTo("Datová zpráva"));
            Assert.That(
                component.FindAll(".ec-preview-data-message dt").Select(static term => term.TextContent),
                Is.EqualTo(["Odesílatel", "Adresát", "Č. j. odesílatele", "Sp. zn. odesílatele", "ID zprávy", "Dodáno", "Doručeno"]),
                "an empty reference is left out");
            Assert.That(definitions[0].TextContent, Does.Contain("Testovací úřad").And.Contain("ID schránky tst0001").And.Contain("Náměstí 1, 100 00 Praha 1"));
            Assert.That(definitions[4].TextContent, Is.EqualTo("1234567"));
            Assert.That(definitions[5].TextContent, Is.EqualTo(MomentDisplay.Text(new DateTimeOffset(2026, 3, 2, 9, 15, 0, TimeSpan.FromHours(1)).UtcDateTime)));
            Assert.That(component.Find(".ec-preview-email-attachments-title").TextContent, Is.EqualTo("Přílohy (2)"));
            Assert.That(
                component.FindAll(".ec-preview-data-message-attachment-open").Select(static button => button.TextContent),
                Is.EqualTo(["Rozhodnutí.pdf", "Poučení.txt"]));
            Assert.That(
                component.FindAll(".ec-preview-data-message-attachment-size").Select(static size => size.TextContent),
                Is.EqualTo([FileSizeDisplay.Text(PdfBytes.Length), FileSizeDisplay.Text(TextBytes.Length)]));
            Assert.That(component.FindAll(".ec-preview-toolbar"), Has.Count.EqualTo(1));
            Assert.That(previews.OpenedTextViews, Is.EqualTo(1));
            Assert.That(component.FindAll("img, iframe, script, a[href]"), Is.Empty);
        }
    }

    [Test]
    public void AReceiptShowsItsEventsWithoutCodesAndNoAttachments()
    {
        using var ctx = NewContext(out _, out _);

        var component = Render(ctx, SyntheticZfo.Receipt());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-data-message .ec-badge").TextContent.Trim(), Is.EqualTo("Doručenka"));
            Assert.That(component.Find(".ec-preview-email-attachments-title").TextContent, Is.EqualTo("Události (3)"));
            Assert.That(
                component.FindAll(".ec-preview-data-message-event-description").Select(static item => item.TextContent),
                Is.EqualTo(["Zpráva byla podána.", "Zpráva byla dodána do schránky.", "Zpráva byla doručena přihlášením oprávněné osoby."]));
            Assert.That(component.FindAll(".ec-preview-data-message-attachments"), Is.Empty);
        }
    }

    [Test]
    public void AnEmptyPartyValueShowsADash()
    {
        using var ctx = NewContext(out _, out _);

        var xml = SyntheticZfo.MessageXml(SyntheticZfo.Namespace, [])
            .Replace("<p:dbIDSender>tst0001</p:dbIDSender>", "<p:dbIDSender/>", StringComparison.Ordinal)
            .Replace("<p:dmSenderAddress>Náměstí 1, 100 00 Praha 1</p:dmSenderAddress>", "", StringComparison.Ordinal);
        var component = Render(ctx, SyntheticZfo.Sign(xml));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-data-message-box")[0].TextContent, Is.EqualTo("ID schránky —"));
            Assert.That(component.FindAll(".ec-preview-data-message-address")[0].TextContent, Is.EqualTo("—"));
        }
    }

    [Test]
    public void AnAttachmentNameOpensItsPreview()
    {
        using var ctx = NewContext(out _, out _);

        var opened = new List<(DataMessage Message, int Index)>();
        var component = Render(ctx, DefaultMessage(), opened.Add);

        component.FindAll(".ec-preview-data-message-attachment-open")[1].Click();

        Assert.That(opened.Select(static item => (item.Message.Attachments[item.Index].Name, item.Index)), Is.EqualTo([("Poučení.txt", 1)]));
    }

    [Test]
    public void AnAttachmentDownloadsUnderItsNameFromItsRowAndOpensNothing()
    {
        using var ctx = NewContext(out _, out var downloader);

        var opened = new List<(DataMessage Message, int Index)>();
        var component = Render(ctx, DefaultMessage(), opened.Add);

        component.Find("[aria-label='Stáhnout Poučení.txt']").Click();

        component.WaitForAssertion(() => Assert.That(downloader.DownloadedContents, Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloader.DownloadedContents[0].FileName, Is.EqualTo("Poučení.txt"));
            Assert.That(downloader.DownloadedContents[0].Content, Is.EqualTo(TextBytes));
            Assert.That(opened, Is.Empty, "the download opens nothing");
        }
    }

    [Test]
    public void AZfoThatIsNoIsdsMessageIsReportedUnreadable()
    {
        using var ctx = NewContext(out _, out _);

        var unreadable = 0;
        var component = ctx.Render<DataMessagePreview>(parameters => parameters
            .Add(static preview => preview.Content, "abc"u8.ToArray())
            .Add(static preview => preview.FileName, "Formulář.zfo")
            .Add(static preview => preview.OnUnreadable, () => unreadable++));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unreadable, Is.EqualTo(1));
            Assert.That(component.FindAll(".ec-preview-data-message"), Is.Empty);
        }
    }

    private static byte[] DefaultMessage()
    {
        return SyntheticZfo.Message(("Rozhodnutí.pdf", PdfBytes), ("Poučení.txt", TextBytes));
    }

    private static BunitContext NewContext(out StubFilePreviews previews, out StubFileDownloader downloader)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        previews = new StubFilePreviews();
        downloader = new StubFileDownloader();
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);

        return ctx;
    }

    private static IRenderedComponent<DataMessagePreview> Render(BunitContext ctx, byte[] content, Action<(DataMessage Message, int Index)>? onOpenAttachment = null)
    {
        return ctx.Render<DataMessagePreview>(parameters => parameters
            .Add(static preview => preview.Content, content)
            .Add(static preview => preview.FileName, "Zpráva.zfo")
            .Add(static preview => preview.OnOpenAttachment, opened => onOpenAttachment?.Invoke(opened)));
    }
}
