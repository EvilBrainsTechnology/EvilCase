using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSizeDisplayTests
{
    [Test]
    public void BytesBelowAKilobyteReadAsBytes()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileSizeDisplay.Text(0), Is.EqualTo("0 B"));
            Assert.That(FileSizeDisplay.Text(1023), Is.EqualTo("1023 B"));
        }
    }

    [Test]
    public void AKilobyteAndUpReadsAsKilobytes()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileSizeDisplay.Text(1024), Is.EqualTo("1 kB"));
            Assert.That(FileSizeDisplay.Text(1536), Is.EqualTo("1.5 kB"));
        }
    }

    [Test]
    public void AMegabyteAndUpReadsAsMegabytes()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileSizeDisplay.Text(1048576), Is.EqualTo("1 MB"));
            Assert.That(FileSizeDisplay.Text(FileLimits.MaxUploadBytes), Is.EqualTo("100 MB"));
        }
    }
}
