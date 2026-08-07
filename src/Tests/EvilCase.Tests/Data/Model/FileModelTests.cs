using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using static EvilBrains.EvilCase.Tests.Data.Model.ModelFixture;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class FileModelTests
{
    [Test]
    public void AFileBelongsToItsPrimaryActAndKeepsTheNameItArrivedWith()
    {
        var asset = Runtime.FindEntityType(typeof(FileAsset));

        Assert.That(asset, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(asset.FindProperty(nameof(FileAsset.ActId))?.IsNullable, Is.False, "a file with no primary act hangs off nothing, and its summary is what explains it");
            Assert.That(asset.FindProperty(nameof(FileAsset.FileName))?.IsNullable, Is.False, "the original name lives with the bytes, not with whoever borrows them");
            Assert.That(IsIndexed(asset, nameof(FileAsset.ActId)), Is.True, "an act reads its own files through this index");
        }
    }

    [Test]
    public void AReferenceOverridesTheOriginalNameWithOneOfItsOwn()
    {
        IReadOnlyEntityType?[] mapped = [Runtime.FindEntityType(typeof(FileAsset)), Runtime.FindEntityType(typeof(ActFileReference))];

        Assert.That(mapped, Has.None.Null, "both the asset and the reference are mapped");

        var asset = mapped[0]!;
        var reference = mapped[1]!;

        var own = reference.FindProperty(nameof(ActFileReference.FileName));
        var original = asset.FindProperty(nameof(FileAsset.FileName));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(own?.IsNullable, Is.False, "a reference names the file where it is read, so it never falls back to the original");
            Assert.That(original?.GetMaxLength(), Is.EqualTo(256), "an unbounded name column is a name nobody sized");
            Assert.That(own?.GetMaxLength(), Is.EqualTo(256), "either name can stand in for the other, so neither column is the shorter one");
            Assert.That(asset.FindNavigation(nameof(FileAsset.References))?.IsCollection, Is.True, "the same PDF filed under five acts is one asset and four references");
        }
    }

    [Test]
    public void AReferenceIsReadFromTheActAndFromTheAsset()
    {
        var reference = Runtime.FindEntityType(typeof(ActFileReference));

        Assert.That(reference, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reference.FindProperty(nameof(ActFileReference.ActId))?.IsNullable, Is.False, "a reference is one act reaching one asset, and it carries both");
            Assert.That(reference.FindProperty(nameof(ActFileReference.FileAssetId))?.IsNullable, Is.False, "a reference is one act reaching one asset, and it carries both");
            Assert.That(IsIndexed(reference, nameof(ActFileReference.ActId)), Is.True, "an act reads the files it borrows through this index");
            Assert.That(IsIndexed(reference, nameof(ActFileReference.FileAssetId)), Is.True, "and which acts reference one asset is a lookup, never a table scan");
        }
    }

    /// <summary>
    /// The three behaviours together are what refuses the delete: the asset goes with its primary act,
    /// an act's own references go with it, and an asset another act still holds aborts the whole delete.
    /// Flip the last one to <c>Cascade</c> and deleting one act silently takes the file from every other.
    /// </summary>
    [Test]
    public void AnActThatOwnsAFileAnotherActReferencesCannotBeDeleted()
    {
        var asset = Runtime.FindEntityType(typeof(FileAsset));
        var reference = Runtime.FindEntityType(typeof(ActFileReference));

        Assert.That(new[] { asset, reference }, Has.None.Null, "both the asset and the reference are mapped");

        var assetToAct = ForeignKeyTo<Act>(asset!);
        var referenceToAct = ForeignKeyTo<Act>(reference!);
        var referenceToAsset = ForeignKeyTo<FileAsset>(reference!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(assetToAct?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "the bytes go with the act they were filed under");
            Assert.That(referenceToAct?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "a reference has no meaning without the act that made it, and dropping it destroys nothing shared");
            Assert.That(referenceToAsset?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "an asset another act still references would otherwise be taken from it by a delete in a fifth case");
        }
    }

    [Test]
    public void DeduplicationStopsAtTheOwner()
    {
        var asset = Runtime.FindEntityType(typeof(FileAsset));

        Assert.That(asset, Is.Not.Null);

        var unique = asset.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] expected = [nameof(FileAsset.OwnerId), nameof(FileAsset.ContentHash)];

        Assert.That(
            unique?.Properties.Select(property => property.Name),
            Is.EqualTo(expected),
            "sharing a row between owners would make one owner's delete another owner's problem");
    }

    /// <summary>
    /// A file is what it is called, never what it is for.
    /// </summary>
    [Test]
    public void NothingAboutAFileIsARole()
    {
        Type[] fileTypes = [typeof(FileAsset), typeof(ActFileReference)];
        var columns = fileTypes.SelectMany(type => ColumnsOf(Runtime.FindEntityType(type))).ToList();
        var kernel = typeof(PartyKind).Assembly.GetTypes().Select(type => type.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Is.Not.Empty, "the file tables are mapped at all, or this test passes vacuously");
            Assert.That(Naming(columns, "Role"), Is.Empty, "a role column is back on a file");
            Assert.That(Naming(kernel, "FileRole"), Is.Empty, "a file role enum is back in the shared kernel");
        }
    }
}
