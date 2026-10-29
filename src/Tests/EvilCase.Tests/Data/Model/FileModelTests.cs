using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class FileModelTests : ModelFixture
{
    [Test]
    public void AFileBelongsToOneCaseOrOneAct()
    {
        var asset = Model.FindEntityType(typeof(FileAsset));
        var designTime = DesignTimeModel.FindEntityType(typeof(FileAsset));

        Assert.That(new[] { asset, designTime }, Has.None.Null);

        var check = designTime!.GetCheckConstraints().SingleOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(asset!.FindProperty(nameof(FileAsset.CaseId))?.IsNullable, Is.True);
            Assert.That(asset.FindProperty(nameof(FileAsset.ActId))?.IsNullable, Is.True);
            Assert.That(check?.Sql, Does.Contain("<>"), "exactly one owner — never both, never neither");
            Assert.That(asset.FindProperty(nameof(FileAsset.FileName))?.IsNullable, Is.False, "the name lives with the bytes, not with whoever borrows them");
            Assert.That(asset.FindProperty(nameof(FileAsset.FileName))?.GetMaxLength(), Is.EqualTo(256));
            Assert.That(IsIndexed(asset, nameof(FileAsset.CaseId)), Is.True, "a case reads its own files through this index");
            Assert.That(IsIndexed(asset, nameof(FileAsset.ActId)), Is.True, "an act reads its own files through this index");
        }
    }

    [Test]
    public void TheStoredPathIsWhatFindsTheBytes()
    {
        var version = Model.FindEntityType(typeof(FileVersion));

        Assert.That(version, Is.Not.Null);

        var storagePath = version!.FindProperty(nameof(FileVersion.StoragePath));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(storagePath?.IsNullable, Is.False, "recomposing the path would lose every blob written under an older layout");
            Assert.That(storagePath?.GetMaxLength(), Is.EqualTo(256));
        }
    }

    [Test]
    public void TheBytesLiveWithTheVersionNotTheFile()
    {
        var columns = ColumnsOf(Model.FindEntityType(typeof(FileAsset)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Is.Not.Empty, "the file table is mapped at all, or this test passes vacuously");
            Assert.That(columns, Does.Not.Contain(nameof(FileVersion.ContentHash)), "the hash belongs to one version");
            Assert.That(columns, Does.Not.Contain(nameof(FileVersion.SizeBytes)), "the size belongs to one version");
            Assert.That(columns, Does.Not.Contain(nameof(FileVersion.MediaType)), "the media type belongs to one version");
            Assert.That(columns, Does.Not.Contain(nameof(FileVersion.StoragePath)), "the blob belongs to one version");
        }
    }

    [Test]
    public void AVersionGoesWithItsFile()
    {
        var version = Model.FindEntityType(typeof(FileVersion));

        Assert.That(version, Is.Not.Null);

        var toFile = ForeignKeyTo<FileAsset>(version!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toFile?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(version!.FindProperty(nameof(FileVersion.FileAssetId))?.IsNullable, Is.False, "a version belongs to a file");
        }
    }

    [Test]
    public void AFileNumbersEachVersionOnce()
    {
        var version = Model.FindEntityType(typeof(FileVersion));

        Assert.That(version, Is.Not.Null);

        var numbering = version!.GetIndexes().SingleOrDefault(static index => index.IsUnique && index.Properties.Count == 3);

        Assert.That(
            numbering?.Properties.Select(static property => property.Name),
            Is.EqualTo([nameof(FileVersion.TenantId), nameof(FileVersion.FileAssetId), nameof(FileVersion.Number)]),
            "two versions of one file must not share a number");
    }

    [Test]
    public void AFileGoesWithWhateverItHangsOn()
    {
        var asset = Model.FindEntityType(typeof(FileAsset));

        Assert.That(asset, Is.Not.Null);

        var toCase = ForeignKeyTo<Case>(asset);
        var toAct = ForeignKeyTo<Act>(asset);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(toAct?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
        }
    }

    [Test]
    public void NothingAboutAFileIsARole()
    {
        var columns = ColumnsOf(Model.FindEntityType(typeof(FileAsset)));
        var kernel = typeof(ContactKind).Assembly.GetTypes().Select(static type => type.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Is.Not.Empty, "the file table is mapped at all, or this test passes vacuously");
            Assert.That(Naming(columns, "Role"), Is.Empty, "a role column is back on a file");
            Assert.That(Naming(kernel, "FileRole"), Is.Empty, "a file role enum is back in the shared kernel");
            Assert.That(Model.FindEntityType("EvilBrains.EvilCase.Data.Entities.ActFileReference"), Is.Null, "a file belongs to exactly one owner");
        }
    }
}
