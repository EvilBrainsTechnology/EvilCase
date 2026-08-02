using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// Runs against a real directory under the system temporary path, created per test and removed after.
/// A store whose whole job is what ends up on disk is not worth testing against a fake filesystem.
/// </summary>
public class LocalFileStoreTests
{
    private string root = null!;

    [SetUp]
    public void CreateRoot()
    {
        this.root = Path.Combine(Path.GetTempPath(), $"evilcase-files-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(this.root);
    }

    [TearDown]
    public void RemoveRoot()
    {
        if (Directory.Exists(this.root))
            Directory.Delete(this.root, recursive: true);
    }

    [Test]
    public async Task StoredContentComesBackByItsHash()
    {
        var store = this.NewStore();

        var stored = await store.Store(Bytes("a submission"));
        await using var read = await store.Open(stored.ContentHash);

        Assert.That(read, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await Text(read), Is.EqualTo("a submission"));
            Assert.That(stored.ContentHash, Is.EqualTo(Sha256Of("a submission")), "the hash is of the content and nothing else");
            Assert.That(stored.SizeBytes, Is.EqualTo(12));
            Assert.That(stored.AlreadyPresent, Is.False);
        }
    }

    [Test]
    public async Task TheSameContentIsWrittenOnceAndReportedAsAlreadyPresent()
    {
        var store = this.NewStore();

        var first = await store.Store(Bytes("a decision"));
        var second = await store.Store(Bytes("a decision"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.ContentHash, Is.EqualTo(first.ContentHash));
            Assert.That(first.AlreadyPresent, Is.False);
            Assert.That(second.AlreadyPresent, Is.True, "which is what makes running an import twice safe");
            Assert.That(Directory.GetFiles(this.root, "*", SearchOption.AllDirectories), Has.Length.EqualTo(1), "one copy on disk");
        }
    }

    [Test]
    public async Task ContentIsFannedOutByTheFirstTwoCharactersOfItsHash()
    {
        var store = this.NewStore();

        var stored = await store.Store(Bytes("anything"));

        var expected = Path.Combine(this.root, stored.ContentHash[..2], stored.ContentHash);

        Assert.That(File.Exists(expected), Is.True, "a flat directory of every blob an owner ever stored is one nobody wants to list");
    }

    [Test]
    public async Task NothingUnfinishedIsLeftBehind()
    {
        var store = this.NewStore();

        _ = await store.Store(Bytes("a decision"));
        _ = await store.Store(Bytes("a decision"));

        var pending = Directory.GetFiles(this.root, ".pending-*", SearchOption.AllDirectories);

        Assert.That(pending, Is.Empty, "a hash promises exact bytes, so a half-written file must never sit under one");
    }

    [Test]
    public async Task ContentThatWasNeverStoredIsAnAnswerRatherThanAnException()
    {
        var store = this.NewStore();

        var absent = Sha256Of("never stored");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await store.Open(absent), Is.Null, "a row can outlive what it pointed at");
            Assert.That(await store.Exists(absent), Is.False);
        }
    }

    [Test]
    public async Task AnEmptyStreamIsContentLikeAnyOther()
    {
        var store = this.NewStore();

        var stored = await store.Store(new MemoryStream());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.SizeBytes, Is.Zero);
            Assert.That(stored.ContentHash, Is.EqualTo(Sha256Of("")));
            Assert.That(await store.Exists(stored.ContentHash), Is.True);
        }
    }

    [Test]
    public void SomethingThatIsNotAHashIsRefused()
    {
        var store = this.NewStore();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => store.Open("../../etc/passwd"), Throws.ArgumentException, "a caller that lost track of what it holds is a bug, not a path to sanitise");
            Assert.That(() => store.Open("NOTAHASH"), Throws.ArgumentException);
            Assert.That(() => store.Open(Sha256Of("x").ToUpperInvariant()), Throws.ArgumentException, "hashes are written lower case, so one case is the only spelling");
        }
    }

    private static MemoryStream Bytes(string content) => new(Encoding.UTF8.GetBytes(content));

    private static string Sha256Of(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    private static async Task<string> Text(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8);

        return await reader.ReadToEndAsync();
    }

    private LocalFileStore NewStore()
    {
        var settings = Options.Create(new FileStoreSettings { RootPath = this.root });

        return new LocalFileStore(settings, new FakeHostEnvironment());
    }

    /// <summary>
    /// The root above is already absolute, so the content root only has to be somewhere real.
    /// </summary>
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "EvilCase.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
