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
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var file = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7m");
        this.ServeSignedFile(file.Versions.Single().StoragePath);

        var response = await this.Reader().ListFileSigners(file.Id, version: null, CancellationToken.None);

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
        var otherFile = await other.AddActFile(await other.AddAct(await other.AddCase(Day), Day), "cizi.pdf.p7m");
        this.ServeSignedFile(otherFile.Versions.Single().StoragePath);

        var response = await this.Reader().ListFileSigners(otherFile.Id, version: null, CancellationToken.None);

        Assert.That(response, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task AnUnknownFileIsNotFound()
    {
        var response = await this.Reader().ListFileSigners(Guid.CreateVersion7(), version: null, CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task AFileWhoseBlobIsGoneIsNotFound()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var file = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7m");

        var response = await this.Reader().ListFileSigners(file.Id, version: null, CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task AFileOverTheLimitIsTooLargeWithoutReadingItsBlob()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var file = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7m", sizeBytes: FileLimits.MaxReadBytes + 1);

        var response = await this.Reader().ListFileSigners(file.Id, version: null, CancellationToken.None);

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
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var pdf = await this.Tenant.AddActFile(act, "Smlouva.pdf");
        var signature = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7s");
        this.ServeSignedFile(signature.Versions.Single().StoragePath, detached: true);

        var response = await this.Reader().ListFileSigners(signature.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile?.FileId, Is.EqualTo(pdf.Id));
            Assert.That(response?.SignedFileProblem, Is.Null);
        }
    }

    [Test]
    public async Task ADetachedSignatureWithoutItsFileSaysSo()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var signature = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7s");
        this.ServeSignedFile(signature.Versions.Single().StoragePath, detached: true);

        var response = await this.Reader().ListFileSigners(signature.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile, Is.Null);
            Assert.That(response?.SignedFileProblem, Is.EqualTo(DetachedSignatureProblem.Missing));
        }
    }

    [Test]
    public async Task AnAttachedSignatureHasNoSignedFile()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        await this.Tenant.AddActFile(act, "Smlouva.pdf");
        var signature = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7m");
        this.ServeSignedFile(signature.Versions.Single().StoragePath);

        var response = await this.Reader().ListFileSigners(signature.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.SignedFile, Is.Null, "a signature with its document inside signs no other file");
            Assert.That(response?.SignedFileProblem, Is.Null);
        }
    }

    [Test]
    public async Task TheSignersOfTheVersionTheRequestNamesAreRead()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var file = await this.Tenant.AddActFile(act, "Smlouva.pdf.p7m");
        this.ServeSignedFile(file.Versions.Single().StoragePath);
        await this.Tenant.AddFileVersion(file);

        var named = await this.Reader().ListFileSigners(file.Id, version: 1, CancellationToken.None);
        var current = await this.Reader().ListFileSigners(file.Id, version: null, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(named?.Signers.Select(static signer => signer.Name), Is.EqualTo(["Testovací podepisující"]));
            Assert.That(current, Is.Null, "the current version is the second one, whose blob is gone");
        }
    }

    private FileSignerReader Reader()
    {
        return new FileSignerReader(this.Tenant.Session, this.blobs);
    }

    private void ServeSignedFile(string storagePath, bool detached = false)
    {
        var signed = SignedFile.Sign("%PDF"u8.ToArray(), detached);

        this.blobs.ReadFileBlob(storagePath).Returns(_ => new MemoryStream(signed, writable: false));
    }
}
