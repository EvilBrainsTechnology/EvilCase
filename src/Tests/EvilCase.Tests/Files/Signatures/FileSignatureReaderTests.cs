using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Frontend;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class FileSignatureReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private static readonly DateTimeOffset Timestamped = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private TestPki pki = null!;

    private IFileBlobStore blobs = null!;

    private MemoryCache cache = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.pki = TestPki.Create();
        this.blobs = Substitute.For<IFileBlobStore>();
        this.cache = new MemoryCache(new MemoryCacheOptions());
    }

    [TearDown]
    public void TearDownReader()
    {
        this.cache.Dispose();
        this.pki.Dispose();
    }

    [Test]
    public async Task AStoredDataMessageReadsItsVerdict()
    {
        var file = await this.AddSealedFile();

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, version: null, CancellationToken.None);

        Assert.That(signature?.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public async Task AnOlderVersionIsReadWhenTheRequestNamesIt()
    {
        var file = await this.AddSealedFile();
        var firstVersion = file.Versions.Single();
        var secondVersion = await this.Tenant.AddFileVersion(file);
        this.ServeSealedMessage(secondVersion.StoragePath);

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, version: 1, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature?.State, Is.EqualTo(FileSignatureState.Valid));
            this.blobs.Received(1).ReadFileBlob(firstVersion.StoragePath);
            this.blobs.DidNotReceive().ReadFileBlob(secondVersion.StoragePath);
        }
    }

    [Test]
    public async Task AMissingVersionIsNotFound()
    {
        var file = await this.AddSealedFile();

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, version: 2, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature, Is.Null);
            this.blobs.DidNotReceiveWithAnyArgs().ReadFileBlob(default!);
        }
    }

    [Test]
    public async Task ANewVersionIsNeverAnsweredFromTheVerdictBeforeIt()
    {
        var file = await this.AddSealedFile();
        var reader = this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        var secondVersion = await this.Tenant.AddFileVersion(file);
        this.ServeSealedMessage(secondVersion.StoragePath);

        await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        this.blobs.Received(1).ReadFileBlob(secondVersion.StoragePath);
    }

    [Test]
    public async Task AFileOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "cizi.zfo");
        this.ServeSealedMessage(otherFile.Versions.Single().StoragePath);

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(otherFile.Id, version: null, CancellationToken.None);

        Assert.That(signature, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task AnUnknownFileIsNotFound()
    {
        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(Guid.CreateVersion7(), version: null, CancellationToken.None);

        Assert.That(signature, Is.Null);
    }

    [Test]
    public async Task AFileWhoseBlobIsGoneIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zprava.zfo");

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, version: null, CancellationToken.None);

        Assert.That(signature, Is.Null);
    }

    [Test]
    public async Task AFileOverTheLimitIsTooLargeWithoutReadingItsBlob()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zprava.zfo", sizeBytes: FileLimits.MaxDataMessageBytes + 1);

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature?.State, Is.EqualTo(FileSignatureState.Invalid));
            Assert.That(signature?.Problem, Is.EqualTo(FileSignatureProblem.TooLarge));
            this.blobs.DidNotReceiveWithAnyArgs().ReadFileBlob(default!);
        }
    }

    [Test]
    public async Task ASecondReadReusesTheVerdict()
    {
        var file = await this.AddSealedFile();
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);
        var reader = this.Reader(revocation);

        await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);
        var second = await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(revocation.Checks, Is.EqualTo(1), "a verified signature is not verified again while cached");
            this.blobs.Received(1).ReadFileBlob(file.Versions.Single().StoragePath);
        }
    }

    [Test]
    public async Task AnUnverifiableVerdictIsAskedForAgain()
    {
        var file = await this.AddSealedFile();
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.Unknown);
        var reader = this.Reader(revocation);

        await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);
        var second = await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(revocation.Checks, Is.EqualTo(2), "an unverifiable verdict is never cached, so the next read asks PostSignum again");
        }
    }

    [Test]
    public async Task ConcurrentReadsOfOneFileShareOneVerification()
    {
        var file = await this.AddSealedFile();
        var zfo = await ReadAll(this.blobs.ReadFileBlob(file.Versions.Single().StoragePath)!);
        var gate = new TaskCompletionSource();
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.blobs.ReadFileBlob(file.Versions.Single().StoragePath).Returns(_ => new GatedStream(zfo, gate.Task, reading));
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.Unknown);
        var reader = this.Reader(revocation);

        var first = reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        // The second read starts once the first is past the database, which one context serves one query at a time.
        await reading.Task;
        var second = reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);
        gate.SetResult();
        await Task.WhenAll(first, second);

        var afterwards = await reader.ReadFileSignature(file.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await first)?.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That((await second)?.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(afterwards?.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(revocation.Checks, Is.EqualTo(2), "two reads in flight share one verification, and a finished one leaves so a retry asks again");
        }
    }

    private static async Task<byte[]> ReadAll(Stream stream)
    {
        await using (stream)
        {
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            return buffer.ToArray();
        }
    }

    private FileSignatureReader Reader(StubSignatureRevocation revocation)
    {
        return new FileSignatureReader(new FixedDbSession(this.Tenant.Context), this.blobs, this.pki.Trust, revocation, this.cache, NullLogger<FileSignatureReader>.Instance);
    }

    private async Task<FileAsset> AddSealedFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zprava.zfo");
        this.ServeSealedMessage(file.Versions.Single().StoragePath);

        return file;
    }

    private void ServeSealedMessage(string storagePath)
    {
        var seal = this.pki.IssueSeal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var zfo = SealedZfo.Seal(SyntheticZfo.MessageXml(SyntheticZfo.Namespace, []), seal, this.pki.IssueTsa(), Timestamped);

        this.blobs.ReadFileBlob(storagePath).Returns(_ => new MemoryStream(zfo, writable: false));
    }
}
