using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileBlobPathTests
{
    [Test]
    public void TheDirectoriesComeFromTheEndOfTheAssetId()
    {
        var tenant = new Guid(0x11111111, 0x1111, 0x7111, 0x81, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11);
        var asset = new Guid(0x198f3d2, 0x1a2b, 0x7c3d, 0x8e, 0x4f, 0xa1, 0xb2, 0xc3, 0xd4, 0xe5, 0xf6);

        var path = FileBlobPath.For(tenant, asset);

        Assert.That(
            path,
            Is.EqualTo("11111111-1111-7111-8111-111111111111/f6/e5/0198f3d2-1a2b-7c3d-8e4f-a1b2c3d4e5f6"),
            "the front of a UUIDv7 is a timestamp and would put a day's uploads in one directory");
    }

    [Test]
    public void TheLayoutIsTenantThenTwoLevelsThenTheBlob()
    {
        var path = FileBlobPath.For(Guid.CreateVersion7(), Guid.CreateVersion7());

        var segments = path.Split('/');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(segments, Has.Length.EqualTo(4));
            Assert.That(segments[1], Has.Length.EqualTo(2));
            Assert.That(segments[2], Has.Length.EqualTo(2));
        }
    }

    [Test]
    public void ThePathIsTheSameOnEveryPlatform()
    {
        var path = FileBlobPath.For(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.That(path, Does.Not.Contain('\\'));
    }

    [Test]
    public void AssetsOfTheSameMillisecondDoNotShareADirectory()
    {
        var tenant = Guid.CreateVersion7();
        var first = new Guid(0x198f3d2, 0x1a2b, 0x7c3d, 0x8e, 0x4f, 0xa1, 0xb2, 0xc3, 0xd4, 0xe5, 0xf6);
        var second = new Guid(0x198f3d2, 0x1a2b, 0x7c3d, 0x8e, 0x4f, 0xa1, 0xb2, 0xc3, 0xd4, 0xa1, 0xa2);

        var firstPath = FileBlobPath.For(tenant, first);
        var secondPath = FileBlobPath.For(tenant, second);

        Assert.That(GetDirectoryPrefix(firstPath), Is.Not.EqualTo(GetDirectoryPrefix(secondPath)));
    }

    private static string GetDirectoryPrefix(string path)
    {
        var segments = path.Split('/');

        return $"{segments[1]}/{segments[2]}";
    }
}
