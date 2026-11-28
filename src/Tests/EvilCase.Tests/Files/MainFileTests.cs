using System.Text;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Frontend;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class MainFileTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 11, 4);

    private FileWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = this.WriterOver(this.Tenant.Context, new FakeFileBlobStore());
    }

    [Test]
    public async Task TheFirstUploadIntoAnEmptyActIsItsMainFileAndTheNextIsNot()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);

        var first = UnionAssert.Holds<UploadedFile>(await this.writer.UploadActFile(act.Id, Upload("smlouva.pdf"), CancellationToken.None));
        var second = UnionAssert.Holds<UploadedFile>(await this.writer.UploadActFile(act.Id, Upload("priloha.pdf"), CancellationToken.None));
        var version = UnionAssert.Holds<UploadedFile>(await this.writer.UploadActFile(act.Id, Upload("Priloha.pdf", content: "b"), CancellationToken.None));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.File.IsMain, Is.True, "the first file of an empty act becomes its main file");
            Assert.That(second.File.IsMain, Is.False, "an upload into an act that has a file does not take the mark");
            Assert.That(version.File.IsMain, Is.False, "a new version never changes the main file");
            Assert.That(await this.MainFileIds(), Is.EqualTo([first.File.FileId]));
        }
    }

    [Test]
    public async Task AnUploadAfterTheMarkWasUnsetDoesNotTakeIt()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var first = UnionAssert.Holds<UploadedFile>(await this.writer.UploadActFile(act.Id, Upload("smlouva.pdf"), CancellationToken.None));

        await this.writer.UnsetMainFile(first.File.FileId, CancellationToken.None);
        var later = UnionAssert.Holds<UploadedFile>(await this.writer.UploadActFile(act.Id, Upload("priloha.pdf"), CancellationToken.None));

        Assert.That(later.File.IsMain, Is.False, "only an upload into an act without any file becomes main");
    }

    [Test]
    public async Task SettingTheMainFileMovesTheMarkWithinItsActAlone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí");
        var otherAct = await this.Tenant.AddAct(@case, Day, "Výzva");
        await this.Tenant.AddActFile(act, "a.pdf", isMain: true);
        var target = await this.Tenant.AddActFile(act, "b.pdf");
        var otherMain = await this.Tenant.AddActFile(otherAct, "c.pdf", isMain: true);

        var outcome = await this.writer.SetMainFile(target.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(MainFileOutcome.Changed));
            Assert.That(await this.MainFileIds(), Is.EquivalentTo([target.Id, otherMain.Id]), "the mark moves within the act and leaves another act's main file be");
        }
    }

    [Test]
    public async Task UnsettingTheMainFileLeavesTheActWithoutOne()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var main = await this.Tenant.AddActFile(act, "a.pdf", isMain: true);
        var other = await this.Tenant.AddActFile(act, "b.pdf");

        var ofOther = await this.writer.UnsetMainFile(other.Id, CancellationToken.None);
        var kept = await this.MainFileIds();
        var ofMain = await this.writer.UnsetMainFile(main.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ofOther, Is.EqualTo(MainFileOutcome.Changed), "unsetting a file that is not main succeeds and changes nothing");
            Assert.That(kept, Is.EqualTo([main.Id]));
            Assert.That(ofMain, Is.EqualTo(MainFileOutcome.Changed));
            Assert.That(await this.MainFileIds(), Is.Empty, "after unsetting, no file of the act is main");
        }
    }

    [Test]
    public async Task DeletingTheMainFileMakesTheOldestRemainingFileMain()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var fileIds = TestTenant.SortedEntityIds(3);

        // Upload order and identifier order disagree, so only the upload moment picks the oldest.
        var main = await this.Tenant.AddActFile(act, "a.pdf", isMain: true);
        var oldest = await this.Tenant.AddActFile(act, "b.pdf", fileId: fileIds[2]);
        await this.Tenant.AddActFile(act, "c.pdf", fileId: fileIds[1]);

        var outcome = await this.writer.DeleteFile(main.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.MainFileIds(), Is.EqualTo([oldest.Id]), "the oldest remaining file, in upload order, takes the mark");
        }
    }

    [Test]
    public async Task DeletingTheMainFileHandsTheMarkOnWithinItsAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var unmarked = await this.Tenant.AddAct(@case, Day, "Výzva");
        await this.Tenant.AddActFile(unmarked, "a.pdf");
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí");
        var main = await this.Tenant.AddActFile(act, "b.pdf", isMain: true);
        var remaining = await this.Tenant.AddActFile(act, "c.pdf");

        await this.writer.DeleteFile(main.Id, CancellationToken.None);

        Assert.That(await this.MainFileIds(), Is.EqualTo([remaining.Id]), "the mark passes to a file of the deleted file's act, never to an older file of another act");
    }

    [Test]
    public async Task DeletingAFileThatIsNotMainLeavesTheMarkAsItIs()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí");
        var main = await this.Tenant.AddActFile(act, "a.pdf", isMain: true);
        var other = await this.Tenant.AddActFile(act, "b.pdf");
        var unmarked = await this.Tenant.AddAct(@case, Day, "Výzva");
        var first = await this.Tenant.AddActFile(unmarked, "c.pdf");
        await this.Tenant.AddActFile(unmarked, "d.pdf");

        await this.writer.DeleteFile(other.Id, CancellationToken.None);
        await this.writer.DeleteFile(first.Id, CancellationToken.None);

        Assert.That(await this.MainFileIds(), Is.EqualTo([main.Id]), "a deleted file that was not main neither moves the mark nor gives one to an act without it");
    }

    [Test]
    public async Task DeletingTheLastFileLeavesNoMainFile()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var main = await this.Tenant.AddActFile(act, "a.pdf", isMain: true);

        var outcome = await this.writer.DeleteFile(main.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(), Is.False);
        }
    }

    [Test]
    public async Task TheDatabaseRefusesASecondMainFileOfOneAct()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        await this.Tenant.AddActFile(act, "a.pdf", isMain: true);

        Assert.That(
            async () => await this.Tenant.AddActFile(act, "b.pdf", isMain: true),
            Throws.InstanceOf<DbUpdateException>(),
            "an act has at most one main file, whatever the writer does");
    }

    [Test]
    public async Task AFileOfAnotherTenantIsNotFoundForTheMark()
    {
        await using var other = await TestTenant.Create();
        var otherFile = await other.AddActFile(await other.AddAct(await other.AddCase(Day), Day), "a.pdf", isMain: true);

        var set = await this.writer.SetMainFile(otherFile.Id, CancellationToken.None);
        var unset = await this.writer.UnsetMainFile(otherFile.Id, CancellationToken.None);
        var stillMain = await other.Context.FileAssets.Where(file => file.Id == otherFile.Id).Select(static file => file.IsMain).SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(set, Is.EqualTo(MainFileOutcome.NotFound), "a file id alone never reaches another tenant's file");
            Assert.That(unset, Is.EqualTo(MainFileOutcome.NotFound), "a file id alone never reaches another tenant's file");
            Assert.That(stillMain, Is.True, "another tenant's main file keeps its mark");
        }
    }

    [Test]
    public async Task TwoConcurrentFirstUploadsLeaveOneMainFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí");
        var store = new MeetingBlobStore(parties: 2);
        await using var second = TestDatabase.CreateMigratedAsHost(this.Tenant.UserContext);

        var results = await Task.WhenAll(
            this.WriterOver(this.Tenant.Context, store).UploadActFile(act.Id, Upload("a.pdf"), CancellationToken.None),
            this.WriterOver(second, store).UploadActFile(act.Id, Upload("b.pdf"), CancellationToken.None));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Select(static result => UnionAssert.Holds<UploadedFile>(result).Kind), Is.All.EqualTo(UploadKind.Uploaded), "the loser of the mark is stored all the same");
            Assert.That(results.Count(static result => UnionAssert.Holds<UploadedFile>(result).File.IsMain), Is.EqualTo(1), "exactly one of two first uploads becomes main");
            Assert.That(await this.MainFileIds(), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task ASetMeetingAConcurrentSetRetriesAndTakesTheMark()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var first = await this.Tenant.AddActFile(act, "a.pdf");
        var second = await this.Tenant.AddActFile(act, "b.pdf");
        await using var other = TestDatabase.CreateMigratedAsHost(this.Tenant.UserContext);
        await using var observer = TestDatabase.CreateMigratedAsHost(this.Tenant.UserContext);

        var logger = new CapturingLogger<FileWriter>();
        var context = this.Tenant.Context;
        var transactional = new FileWriter(new TransactionalDbSession(context), new FakeFileBlobStore(), this.Tenant.UserContext, logger);

        // An open connection keeps the writer on one backend the observer can watch.
        await context.Database.OpenConnectionAsync();
        var writerPid = await context.Database
            .SqlQuery<int>($"""SELECT pg_backend_pid() AS "Value" """)
            .SingleAsync();

        MainFileOutcome outcome;

        await using (var transaction = await other.Database.BeginTransactionAsync())
        {
            await other.FileAssets
                .Where(file => file.Id == first.Id)
                .ExecuteUpdateAsync(static setters => setters.SetProperty(static file => file.IsMain, valueExpression: true));

            // The set waits on the uncommitted mark, then loses the unique index to it.
            var setting = transactional.SetMainFile(second.Id, CancellationToken.None);

            while (!setting.IsCompleted && !await WaitsOnALock(observer, writerPid))
                await Task.Delay(TimeSpan.FromMilliseconds(10));

            await transaction.CommitAsync();

            outcome = await setting;
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(MainFileOutcome.Changed));
            Assert.That(logger.LoggedWarningOrAbove, Is.True, "the set lost the unique index to the concurrent mark and retried");
            Assert.That(await this.MainFileIds(), Is.EqualTo([second.Id]), "the set retried after the concurrent mark committed takes it over");
        }
    }

    private static async Task<bool> WaitsOnALock(ApplicationDbContext observer, int pid)
    {
        return await observer.Database
            .SqlQuery<bool>($"""SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE pid = {pid} AND wait_event_type = 'Lock') AS "Value" """)
            .SingleAsync();
    }

    private static FileUpload Upload(string fileName, string content = "a")
    {
        return new FileUpload { FileName = fileName, MediaType = "application/pdf", Content = new MemoryStream(Encoding.UTF8.GetBytes(content)) };
    }

    private async Task<List<Guid>> MainFileIds()
    {
        return await this.Tenant.Context.FileAssets
            .AsNoTracking()
            .Where(static file => file.IsMain)
            .Select(static file => file.Id)
            .ToListAsync();
    }

    private FileWriter WriterOver(ApplicationDbContext context, IFileBlobStore store)
    {
        return new FileWriter(new FixedDbSession(context), store, this.Tenant.UserContext, NullLogger<FileWriter>.Instance);
    }
}
