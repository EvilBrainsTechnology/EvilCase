using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.Tests.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class SignedDataTests
{
    private static readonly byte[] Content = "%PDF-1.4"u8.ToArray();

    [Test]
    public void AnAttachedSignatureCarriesItsContent()
    {
        var signed = SignedData.ParseOrDefault(SignedFile.Sign(Content, detached: false));

        Assert.That(signed?.Content, Is.EqualTo(Content));
    }

    [Test]
    public void ADetachedSignatureCarriesNoContent()
    {
        var signed = SignedData.ParseOrDefault(SignedFile.Sign(Content, detached: true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signed, Is.Not.Null);
            Assert.That(signed?.Content, Is.Null);
        }
    }

    [Test]
    public void BytesThatAreNoSignedDataReadAsNothing()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(SignedData.ParseOrDefault("abc"u8.ToArray()), Is.Null);
            Assert.That(SignedData.ParseOrDefault([]), Is.Null);
        }
    }
}
