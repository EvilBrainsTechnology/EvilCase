using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class FileSignerVerifierTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private static readonly DateTime Now = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);

    private static readonly byte[] Pdf = "%PDF-1.4"u8.ToArray();

    private TestPki pki = null!;

    private IFileBlobStore blobs = null!;

    private MemoryCache cache = null!;

    [SetUp]
    public void SetUpVerifier()
    {
        this.pki = TestPki.Create();
        this.blobs = Substitute.For<IFileBlobStore>();
        this.cache = new MemoryCache(new MemoryCacheOptions());
    }

    [TearDown]
    public void TearDownVerifier()
    {
        this.cache.Dispose();
        this.pki.Dispose();
    }

    [Test]
    public async Task AnAttachedSignatureReadsItsVerdict()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");
        this.Serve(signature, this.Sign(Pdf, detached: false));

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(response?.SignedFile, Is.Null);
        }
    }

    [Test]
    public async Task ADetachedSignatureIsVerifiedAgainstTheFileOfItsName()
    {
        var (pdf, signature) = await this.AddDetachedPair();

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(response?.SignedFile?.FileId, Is.EqualTo(pdf.Id));
        }
    }

    [Test]
    public async Task ADetachedSignatureOverOtherContentIsInvalid()
    {
        var (pdf, signature) = await this.AddDetachedPair();
        this.Serve(pdf, "%PDF-1.5"u8.ToArray());

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        Assert.That(response?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.ContentMismatch));
    }

    [Test]
    public async Task ADetachedSignatureWithoutItsFileIsUnverifiable()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7s");
        this.Serve(signature, this.Sign(Pdf, detached: true));

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Signers.Select(static signer => signer.Problem), Is.All.EqualTo(FileSignerProblem.SignedFileMissing));
            Assert.That(response?.SignedFileProblem, Is.EqualTo(DetachedSignatureProblem.Missing));
            this.blobs.Received(1).ReadFileBlob(Arg.Any<string>());
        }
    }

    [Test]
    public async Task TwoFilesOfTheSignedNameAreAmbiguous()
    {
        var (_, signature) = await this.AddDetachedPair();
        await this.Tenant.AddCaseFile(await this.Tenant.Context.Cases.SingleAsync(), "Smlouva.pdf");

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.SignedFileAmbiguous));
            Assert.That(response?.SignedFileProblem, Is.EqualTo(DetachedSignatureProblem.Ambiguous));
        }
    }

    [Test]
    public async Task AFileOfTheSignedNameOnAnotherCaseIsNotPaired()
    {
        var caseA = await this.Tenant.AddCase(Day);
        var caseB = await this.Tenant.AddCase(Day);
        var pdf = await this.Tenant.AddCaseFile(caseA, "Smlouva.pdf");
        var signature = await this.Tenant.AddCaseFile(caseB, "Smlouva.pdf.p7s");
        this.Serve(pdf, Pdf);
        this.Serve(signature, this.Sign(Pdf, detached: true));

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        Assert.That(response?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.SignedFileMissing), "a signature pairs only within its own case or act");
    }

    [Test]
    public async Task ASignedFileOverTheLimitIsNotRead()
    {
        var @case = await this.Tenant.AddCase(Day);
        var pdf = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf", sizeBytes: FileLimits.MaxSignedFileBytes + 1);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7s");
        this.Serve(pdf, Pdf);
        this.Serve(signature, this.Sign(Pdf, detached: true));

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.SignedFileTooLarge));
            Assert.That(response?.SignedFile?.FileId, Is.EqualTo(pdf.Id));
            this.blobs.DidNotReceive().ReadFileBlob(pdf.StoragePath);
        }
    }

    [Test]
    public async Task ASecondReadReusesTheVerdict()
    {
        var (_, signature) = await this.AddDetachedPair();
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);
        var verifier = this.Verifier(revocation);

        await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);
        var second = await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(revocation.Checks, Is.EqualTo(1), "a verified signature is not verified again while cached");
            this.blobs.Received(1).ReadFileBlob(signature.StoragePath);
        }
    }

    [Test]
    public async Task AnotherSignedFileUnderTheNameIsVerifiedAgain()
    {
        var (pdf, signature) = await this.AddDetachedPair();
        var verifier = this.Verifier();

        var first = await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);

        await this.Tenant.Context.FileAssets
            .Where(file => file.Id == pdf.Id)
            .ExecuteDeleteAsync();

        var replacement = await this.Tenant.AddCaseFile(await this.Tenant.Context.Cases.SingleAsync(), "Smlouva.pdf");
        this.Serve(replacement, "%PDF-1.5"u8.ToArray());

        var second = await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(second?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.ContentMismatch), "the cached verdict belongs to the pair, not the signature alone");
        }
    }

    [Test]
    public async Task AnUnverifiableVerdictIsAskedForAgain()
    {
        var (_, signature) = await this.AddDetachedPair();
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.Unknown);
        var verifier = this.Verifier(revocation);

        await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);
        var second = await verifier.VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second?.Signers.Single().Problem, Is.EqualTo(FileSignerProblem.RevocationUnknown));
            Assert.That(revocation.Checks, Is.EqualTo(2), "an unverifiable verdict is never cached, so the next read asks the authority again");
        }
    }

    [Test]
    public async Task ConcurrentReadsOfOneFileShareOneVerification()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");
        var signed = this.Sign(Pdf, detached: false);
        var gate = new TaskCompletionSource();
        var reading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        this.blobs.ReadFileBlob(signature.StoragePath).Returns(_ => new GatedStream(signed, gate.Task, reading));
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);
        var verifier = this.Verifier(revocation);

        var first = verifier.VerifyFileSigners(signature.Id, CancellationToken.None);

        // The second read starts once the first is past the database, which one context serves one query at a time; it
        // joins the flight or, where the first already finished, reads the cached verdict.
        await reading.Task;
        var second = verifier.VerifyFileSigners(signature.Id, CancellationToken.None);
        gate.SetResult();
        await Task.WhenAll(first, second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await first)?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That((await second)?.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(revocation.Checks, Is.EqualTo(1), "two reads in flight share one verification");
            this.blobs.Received(1).ReadFileBlob(signature.StoragePath);
        }
    }

    [Test]
    public async Task AFileOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "cizi.pdf.p7m");
        this.Serve(otherFile, this.Sign(Pdf, detached: false));

        var response = await this.Verifier().VerifyFileSigners(otherFile.Id, CancellationToken.None);

        Assert.That(response, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task AnUnknownFileIsNotFound()
    {
        var response = await this.Verifier().VerifyFileSigners(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task AFileWhoseBlobIsGoneIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m");

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        Assert.That(response, Is.Null);
    }

    [Test]
    public async Task ASignatureOverTheLimitIsTooLargeWithoutReadingItsBlob()
    {
        var @case = await this.Tenant.AddCase(Day);
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7m", sizeBytes: FileLimits.MaxSignedFileBytes + 1);

        var response = await this.Verifier().VerifyFileSigners(signature.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response?.Problem, Is.EqualTo(FileSignersProblem.TooLarge));
            this.blobs.DidNotReceiveWithAnyArgs().ReadFileBlob(default!);
        }
    }

    private FileSignerVerifier Verifier(StubSignatureRevocation? revocation = null)
    {
        return new FileSignerVerifier(
            new FixedDbSession(this.Tenant.Context),
            this.blobs,
            this.pki.Trust,
            revocation ?? new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked),
            new TestTimeProvider(Now),
            this.cache,
            NullLogger<FileSignerVerifier>.Instance);
    }

    private async Task<(FileAsset Pdf, FileAsset Signature)> AddDetachedPair()
    {
        var @case = await this.Tenant.AddCase(Day);
        var pdf = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf");
        var signature = await this.Tenant.AddCaseFile(@case, "Smlouva.pdf.p7s");
        this.Serve(pdf, Pdf);
        this.Serve(signature, this.Sign(Pdf, detached: true));

        return (pdf, signature);
    }

    private byte[] Sign(byte[] content, bool detached)
    {
        var certificate = this.pki.IssueSeal(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            commonName: "Jan Testovací",
            policy: "0.4.0.194112.1.2");

        return SignedFile.Sign(content, detached, [certificate]);
    }

    private void Serve(FileAsset file, byte[] content)
    {
        this.blobs.ReadFileBlob(file.StoragePath).Returns(_ => new MemoryStream(content, writable: false));
    }
}
