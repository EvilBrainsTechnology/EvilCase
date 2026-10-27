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

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, CancellationToken.None);

        Assert.That(signature?.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public async Task AFileOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "cizi.zfo");
        this.ServeSealedMessage(otherFile.StoragePath);

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(otherFile.Id, CancellationToken.None);

        Assert.That(signature, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task AnUnknownFileIsNotFound()
    {
        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(signature, Is.Null);
    }

    [Test]
    public async Task AFileWhoseBlobIsGoneIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zprava.zfo");

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, CancellationToken.None);

        Assert.That(signature, Is.Null);
    }

    [Test]
    public async Task AFileOverTheLimitIsTooLargeWithoutReadingItsBlob()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zprava.zfo", sizeBytes: FileLimits.MaxDataMessageBytes + 1);

        var signature = await this.Reader(new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked)).ReadFileSignature(file.Id, CancellationToken.None);

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

        await reader.ReadFileSignature(file.Id, CancellationToken.None);
        var second = await reader.ReadFileSignature(file.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(revocation.Checks, Is.EqualTo(1), "a verified signature is not verified again while cached");
            this.blobs.Received(1).ReadFileBlob(file.StoragePath);
        }
    }

    [Test]
    public async Task AnUnverifiableVerdictIsAskedForAgain()
    {
        var file = await this.AddSealedFile();
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.Unknown);
        var reader = this.Reader(revocation);

        await reader.ReadFileSignature(file.Id, CancellationToken.None);
        var second = await reader.ReadFileSignature(file.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(revocation.Checks, Is.EqualTo(2), "an unverifiable verdict is never cached, so the next read asks PostSignum again");
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
        this.ServeSealedMessage(file.StoragePath);

        return file;
    }

    private void ServeSealedMessage(string storagePath)
    {
        var seal = this.pki.IssueSeal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var zfo = SealedZfo.Seal(SyntheticZfo.MessageXml(SyntheticZfo.Namespace, []), seal, this.pki.IssueTsa(), Timestamped);

        this.blobs.ReadFileBlob(storagePath).Returns(_ => new MemoryStream(zfo, writable: false));
    }
}
