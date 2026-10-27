using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class FileSignerReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private IFileBlobStore blobs = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.blobs = Substitute.For<IFileBlobStore>();
    }

    [Test]
    public async Task AStoredSignedFileListsItsSigners()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");
        this.ServeSignedFile(file.StoragePath);

        var response = await this.Reader().ListFileSigners(file.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Problem, Is.Null);
            Assert.That(response?.Signers.Select(static signer => signer.Name), Is.EqualTo(["Testovací podepisující"]));
        }
    }

    [Test]
    public async Task AFileOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "cizi.pdf.p7m");
        this.ServeSignedFile(otherFile.StoragePath);

        var response = await this.Reader().ListFileSigners(otherFile.Id, CancellationToken.None);

        Assert.That(response, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task AnUnknownFileIsNotFound()
    {
        var response = await this.Reader().ListFileSigners(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task AFileWhoseBlobIsGoneIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");

        var response = await this.Reader().ListFileSigners(file.Id, CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task AFileOverTheLimitIsTooLargeWithoutReadingItsBlob()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m", sizeBytes: FileLimits.MaxSignedFileBytes + 1);

        var response = await this.Reader().ListFileSigners(file.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Problem, Is.EqualTo(FileSignersProblem.TooLarge));
            Assert.That(response?.Signers, Is.Empty);
            this.blobs.DidNotReceiveWithAnyArgs().ReadFileBlob(default!);
        }
    }

    [Test]
    public async Task ADetachedSignatureNamesItsSignedFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        var pdf = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf");
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7s");
        this.ServeSignedFile(signature.StoragePath, detached: true);

        var response = await this.Reader().ListFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile?.FileId, Is.EqualTo(pdf.Id));
            Assert.That(response?.SignedFileProblem, Is.Null);
        }
    }

    [Test]
    public async Task ADetachedSignatureWithoutItsFileSaysSo()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7s");
        this.ServeSignedFile(signature.StoragePath, detached: true);

        var response = await this.Reader().ListFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile, Is.Null);
            Assert.That(response?.SignedFileProblem, Is.EqualTo(DetachedSignatureProblem.Missing));
        }
    }

    [Test]
    public async Task AnAttachedSignatureHasNoSignedFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddCaseFile(@case, "Smlouva.pdf");
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");
        this.ServeSignedFile(signature.StoragePath);

        var response = await this.Reader().ListFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile, Is.Null, "a signature with its document inside signs no other file");
            Assert.That(response?.SignedFileProblem, Is.Null);
        }
    }

    private FileSignerReader Reader()
    {
        return new FileSignerReader(new FixedDbSession(this.Tenant.Context), this.blobs);
    }

    private void ServeSignedFile(string storagePath, bool detached = false)
    {
        var signed = SignedFile.Sign("%PDF"u8.ToArray(), detached);

        this.blobs.ReadFileBlob(storagePath).Returns(_ => new MemoryStream(signed, writable: false));
    }
}
