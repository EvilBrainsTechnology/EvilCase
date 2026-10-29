using System.Formats.Asn1;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class DetachedSignaturesTests
{
    [TestCase("Smlouva.pdf.p7s", ExpectedResult = "Smlouva.pdf")]
    [TestCase("Smlouva.pdf.P7M", ExpectedResult = "Smlouva.pdf")]
    [TestCase("Smlouva.p7s", ExpectedResult = "Smlouva")]
    [TestCase(".p7s", ExpectedResult = null)]
    [TestCase("Smlouva.pdf", ExpectedResult = null)]
    public string? TheSignedNameIsTheNameWithoutTheSignatureExtension(string fileName)
    {
        return DetachedSignatures.SignedFileName(fileName);
    }

    [Test]
    public void TheFileOfTheSignedNameCarriesItsSignature()
    {
        var pdf = Item("Smlouva.pdf");
        var signature = Item("Smlouva.pdf.p7s");
        var other = Item("Jiny.pdf");

        var files = DetachedSignatures.WithSignatureFiles([pdf, signature, other]);

        Assert.That(files.Select(static file => file.SignatureFileId), Is.EqualTo(new Guid?[] { signature.FileId, null, null }));
    }

    [Test]
    public void AnUpperCaseExtensionPairs()
    {
        var pdf = Item("Smlouva.pdf");
        var signature = Item("Smlouva.pdf.P7S");

        var files = DetachedSignatures.WithSignatureFiles([pdf, signature]);

        Assert.That(files[0].SignatureFileId, Is.EqualTo(signature.FileId));
    }

    [Test]
    public void ANameDifferingInCaseDoesNotPair()
    {
        var files = DetachedSignatures.WithSignatureFiles([Item("smlouva.pdf"), Item("Smlouva.pdf.p7s")]);

        Assert.That(files.Select(static file => file.SignatureFileId), Is.All.Null, "the name before the extension must match exactly");
    }

    [Test]
    public void TwoFilesOfTheSignedNameLeaveTheSignatureWithoutOne()
    {
        var files = DetachedSignatures.WithSignatureFiles([Item("Smlouva.pdf"), Item("Smlouva.pdf"), Item("Smlouva.pdf.p7s")]);

        Assert.That(files.Select(static file => file.SignatureFileId), Is.All.Null);
    }

    [Test]
    public void TwoSignaturesOfOneNameLeaveTheFileWithoutOne()
    {
        var files = DetachedSignatures.WithSignatureFiles([Item("Smlouva.pdf"), Item("Smlouva.pdf.p7s"), Item("Smlouva.pdf.p7m")]);

        Assert.That(files.Select(static file => file.SignatureFileId), Is.All.Null);
    }

    [Test]
    public void ASignatureWithoutExtensionPairsWithTheBareName()
    {
        var bare = Item("Smlouva");
        var signature = Item("Smlouva.p7s");

        var files = DetachedSignatures.WithSignatureFiles([bare, Item("Smlouva.pdf"), signature]);

        Assert.That(files.Select(static file => file.SignatureFileId), Is.EqualTo(new Guid?[] { signature.FileId, null, null }));
    }

    [Test]
    public void PairNamesAMissingFile()
    {
        var signature = Item("Smlouva.pdf.p7s");

        var pair = DetachedSignatures.Pair(signature.FileId, signature.FileName, [signature, Item("Jiny.pdf")]);

        Assert.That(pair, Is.EqualTo(((FileListItem?)null, (DetachedSignatureProblem?)DetachedSignatureProblem.Missing)));
    }

    [Test]
    public void PairNamesAnAmbiguousFile()
    {
        var signature = Item("Smlouva.pdf.p7s");

        var pair = DetachedSignatures.Pair(signature.FileId, signature.FileName, [signature, Item("Smlouva.pdf"), Item("Smlouva.pdf")]);

        Assert.That(pair, Is.EqualTo(((FileListItem?)null, (DetachedSignatureProblem?)DetachedSignatureProblem.Ambiguous)));
    }

    [Test]
    public void PairReturnsTheSignedFile()
    {
        var pdf = Item("Smlouva.pdf");
        var signature = Item("Smlouva.pdf.p7s");

        var (signedFile, problem) = DetachedSignatures.Pair(signature.FileId, signature.FileName, [pdf, signature]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signedFile, Is.EqualTo(pdf with { SignatureFileId = signature.FileId }));
            Assert.That(problem, Is.Null);
        }
    }

    [Test]
    public void ADetachedSignatureIsDetached()
    {
        Assert.That(DetachedSignatures.IsDetached(SignedFile.Sign("%PDF"u8.ToArray(), detached: true)), Is.True);
    }

    [Test]
    public void AnAttachedSignatureIsNotDetached()
    {
        Assert.That(DetachedSignatures.IsDetached(SignedFile.Sign("%PDF"u8.ToArray(), detached: false)), Is.False);
    }

    [Test]
    public void AnAttachedSignatureOverEmptyContentIsNotDetached()
    {
        // SignedCms refuses to sign empty content, so the SignedData is written up to its encapsulated content.
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier("1.2.840.113549.1.7.2");
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
            using (writer.PushSequence())
            {
                writer.WriteInteger(1);
                writer.WriteEncodedValue([0x31, 0x00]); // digestAlgorithms, empty
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier("1.2.840.113549.1.7.1");
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                        writer.WriteOctetString([]);
                }
            }
        }

        Assert.That(DetachedSignatures.IsDetached(writer.Encode()), Is.False, "empty encapsulated content is still content");
    }

    [Test]
    public void ContentOfAnotherTypeIsNotDetached()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier("1.2.840.113549.1.7.1");
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0)))
                writer.WriteOctetString([]);
        }

        Assert.That(DetachedSignatures.IsDetached(writer.Encode()), Is.False);
    }

    [Test]
    public void ADetachedSignatureFollowedByOtherBytesIsDetached()
    {
        Assert.That(DetachedSignatures.IsDetached((byte[])[.. SignedFile.Sign("%PDF"u8.ToArray(), detached: true), 0x00, 0x00]), Is.True);
    }

    [Test]
    public void ASignedDataContentInfoWithoutContentIsNotDetached()
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
            writer.WriteObjectIdentifier("1.2.840.113549.1.7.2");

        Assert.That(DetachedSignatures.IsDetached(writer.Encode()), Is.False);
    }

    [Test]
    public void BytesThatAreNoCmsAreNotDetached()
    {
        Assert.That(DetachedSignatures.IsDetached("no cms"u8.ToArray()), Is.False);
    }

    private static FileListItem Item(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.CreateVersion7(),
            FileName = fileName,
            SizeBytes = 1,
            Created = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc),
            Version = 1,
            VersionCount = 1,
        };
    }
}
