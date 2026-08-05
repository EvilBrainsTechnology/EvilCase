using System.Reflection;
using System.Runtime.CompilerServices;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Builds the model without touching a server — the design-time factory names no connection string,
/// and nothing here opens one. What it pins are the conventions in
/// <c>.claude/rules/data.md</c>, which a new entity is otherwise free to forget silently.
/// </summary>
public class ApplicationDbModelTests
{
    [Test]
    public void EveryEnumIsStoredAsAName()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var enumProperties = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
            .ToList();

        Assert.That(enumProperties, Is.Not.Empty, "the model has enums at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var property in enumProperties)
            {
                var name = $"{property.DeclaringType.ShortName()}.{property.Name}";

                Assert.That(property.GetProviderClrType(), Is.EqualTo(typeof(string)), $"{name} is stored by number, so renumbering the enum would silently rewrite every row");
                Assert.That(property.GetColumnType(), Does.StartWith("character varying"), $"{name} is stored as unbounded text rather than a bounded column");
            }
        }
    }

    [Test]
    public void EveryAggregateRootCarriesItsOwner()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var party = context.Model.FindEntityType(typeof(Party));

        Assert.That(party, Is.Not.Null);

        var owner = party.FindProperty(nameof(Party.OwnerId));
        var ownerForeignKey = party.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(User));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(owner, Is.Not.Null, "the column ships before anything filters on it");
            Assert.That(owner?.IsNullable, Is.False, "a party without an owner is unreachable once M8 filters");
            Assert.That(ownerForeignKey, Is.Not.Null, "and it points at a real user");
            Assert.That(IsIndexed(party, nameof(Party.OwnerId)), Is.True, "every owner-scoped query reads this index");
        }
    }

    [Test]
    public void APartyIsFlatAndItsAddressIsOneBlock()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var party = context.Model.FindEntityType(typeof(Party));

        Assert.That(party, Is.Not.Null);

        var columns = party.GetProperties().Select(property => property.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(party.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Party)), Is.False, "an official carries no link to its authority");
            Assert.That(columns, Has.Member(nameof(Party.Address)), "the address is one free-text block");
            Assert.That(columns, Does.Not.Contain("Town").And.Not.Contains("PostCode"), "and is never split into parts");
            Assert.That(party.FindProperty(nameof(Party.Kind))?.ClrType, Is.EqualTo(typeof(PartyKind)));
            Assert.That(IsIndexed(party, nameof(Party.DataBoxId)), Is.True, "looking a party up by data box is the one unambiguous lookup");
        }
    }

    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage names rather than the
    /// CLR ones — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void EveryIdentifierIsStoredUnderTheNameTheVisionGivesIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var @case = context.Model.FindEntityType(typeof(Case));
        var act = context.Model.FindEntityType(typeof(Act));
        var external = context.Model.FindEntityType(typeof(ExternalCaseNumber));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ColumnsOf(@case), Has.Member("CaseNumber"), "the case's own mark is stored in a column named CaseNumber");
            Assert.That(ColumnsOf(act), Has.Member("ExternalActNumber"), "the number the issuing authority gave an act is stored in a column named ExternalActNumber");
            Assert.That(external?.GetTableName(), Is.EqualTo("ExternalCaseNumbers"), "a mark somebody else assigned is a row of the ExternalCaseNumbers table");
        }
    }

    [Test]
    public void TheCasesOwnNumberIsAColumnOnTheCase()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var @case = context.Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        var mark = @case.FindProperty(nameof(Case.CaseNumber));
        var unique = @case.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] expected = [nameof(Case.OwnerId), nameof(Case.CaseNumber)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mark, Is.Not.Null, "a case always has exactly one of its own, so it is a column and not a row");
            Assert.That(mark?.IsNullable, Is.False, "it is generated with the case and never absent");
            Assert.That(unique?.Properties.Select(property => property.Name), Is.EqualTo(expected), "and a generated series must not repeat within one owner");
        }
    }

    [Test]
    public void ACaseHangsUnderNothing()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var @case = context.Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.FindProperty("ParentCaseId"), Is.Null, "a case relates to another case, and neither of them is above the other");
            Assert.That(@case.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Case)), Is.False, "a self-reference is a hierarchy, whatever it is called");
        }
    }

    [Test]
    public void ARelationIsOneRowPerOrderedPairOfDistinctCases()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        // The read-optimized model drops check constraints; only the design-time one carries them.
        var designTime = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(CaseRelation));
        var relation = context.Model.FindEntityType(typeof(CaseRelation));

        Assert.That(new[] { designTime, relation }, Has.None.Null, "the relation is mapped");

        var check = designTime!.GetCheckConstraints().SingleOrDefault();
        var unique = relation!.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] pair = [nameof(CaseRelation.CaseId), nameof(CaseRelation.RelatedCaseId)];
        string[] bare = ["Id", .. pair];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                check?.Sql?.Replace(" ", "", StringComparison.Ordinal),
                Is.AnyOf(@"""CaseId""<""RelatedCaseId""", @"""RelatedCaseId"">""CaseId"""),
                "the pair is stored in one order, which is also what refuses a case related to itself");
            Assert.That(unique?.Properties.Select(property => property.Name), Is.EqualTo(pair), "one pair is one row, whichever end asks");
            Assert.That(IsIndexed(relation, nameof(CaseRelation.RelatedCaseId)), Is.True, "a relation is read from either end, so both columns are indexed");
            Assert.That(ColumnsOf(relation), Is.EquivalentTo(bare), "the row is bare — it carries the pair and nothing else");
            Assert.That(relation.GetNavigations(), Is.Empty, "the two ends are the same kind of end, so a read names both columns rather than following one of them");
        }
    }

    /// <summary>
    /// The two cascades are what makes the delete symmetric: the relation goes from whichever end is
    /// deleted, and neither of them reaches the case at the other end.
    /// </summary>
    [Test]
    public void DeletingACaseTakesItsRelationsAndLeavesTheCasesItRelatedTo()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var relation = context.Model.FindEntityType(typeof(CaseRelation));

        Assert.That(relation, Is.Not.Null);

        var toCases = relation.GetForeignKeys().Where(key => key.PrincipalEntityType.ClrType == typeof(Case)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toCases, Has.Count.EqualTo(2), "a relation names both ends");
            Assert.That(toCases.TrueForAll(key => key.DeleteBehavior == DeleteBehavior.Cascade), Is.True, "a relation has no meaning without either of its cases");
            Assert.That(
                context.Model.GetEntityTypes().Any(entityType => entityType.ClrType == typeof(Case)
                    && entityType.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Case))),
                Is.False,
                "nothing cascades from one case to another, so a delete stops at the relation");
        }
    }

    [Test]
    public void AnActCarriesOneMandatoryDateAndNoOrderingNumber()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var date = act.FindProperty(nameof(Act.Date));
        var others = act.GetProperties()
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateOnly)
                && !string.Equals(property.Name, nameof(Act.Date), StringComparison.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(date?.ClrType, Is.EqualTo(typeof(DateOnly)), "the act date is a calendar date, and the hour never enters the period arithmetic it starts");
            Assert.That(typeof(Act).GetProperty(nameof(Act.Date))?.GetCustomAttribute<RequiredMemberAttribute>(), Is.Not.Null, "an act cannot be constructed without its date");
            Assert.That(others.Select(property => property.Name), Is.Empty, "the act date is the only date an act carries");
            Assert.That(act.FindProperty("Ordinal"), Is.Null, "an act is ordered by its date alone, so it carries no ordering number");
        }
    }

    [Test]
    public void TheActSummaryIsAsLongAsItNeedsToBe()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        Assert.That(act.FindProperty(nameof(Act.Summary))?.GetMaxLength(), Is.Null, "the summary is long-form and lives on the act alone");
    }

    [Test]
    public void ActsAreIndexedForOrderingByDateWithinACase()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        string[] expected = [nameof(Act.CaseId), nameof(Act.Date)];
        var byDate = act.GetIndexes().SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(expected, StringComparer.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byDate, Is.Not.Null, "an act list reads one case ordered by the act date, and (CaseId, Date) is what serves it");
            Assert.That(byDate?.IsUnique, Is.False, "two acts of one case share a date whenever they were filed on the same day");
        }
    }

    [Test]
    public void EveryExternalMarkNamesWhoAssignedIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var external = context.Model.FindEntityType(typeof(ExternalCaseNumber));

        Assert.That(external, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                external.FindProperty(nameof(ExternalCaseNumber.AssignedByPartyId))?.IsNullable,
                Is.False,
                "a mark nobody assigned is the case's own, and that one lives on the case");

            Assert.That(
                external.GetIndexes().Any(index => index.GetFilter() is not null),
                Is.False,
                "nothing here is conditional any more — this table is external marks and only those");
        }
    }

    [Test]
    public void AMarkNeverTakesAPartyDownWithIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var external = context.Model.FindEntityType(typeof(ExternalCaseNumber));

        Assert.That(external, Is.Not.Null);

        var toParty = external.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Party));
        var toCase = external.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParty?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "a party accumulates history across cases and outlives any one mark naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "a mark has no meaning without its case");
            Assert.That(external.GetProperties().Any(property => string.Equals(property.Name, "OwnerId", StringComparison.Ordinal)), Is.False, "only aggregate roots carry an owner, and a mark is not one");
        }
    }

    [Test]
    public void AnActNeverTakesAPartyDownWithIt()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var act = context.Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var toParties = act.GetForeignKeys().Where(key => key.PrincipalEntityType.ClrType == typeof(Party)).ToList();
        var toCase = act.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParties, Has.Count.EqualTo(2), "an act references who issued it and who it was addressed to");
            Assert.That(toParties.TrueForAll(key => key.DeleteBehavior == DeleteBehavior.Restrict), Is.True, "a party outlives any one act naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "an act has no meaning without its case");
        }
    }

    /// <summary>
    /// From review on #86: a one-to-many is reachable from both ends, so the principal carries a
    /// collection rather than the dependent carrying the only reference. Without it a party's history
    /// across cases can be reached only by querying the dependent table by hand.
    /// </summary>
    [Test]
    public void EveryOneToManyIsNavigableFromBothEnds()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var oneToMany = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys())
            .Where(key => !key.IsUnique && key.DependentToPrincipal is not null && key.PrincipalEntityType.ClrType != typeof(User))
            .ToList();

        Assert.That(oneToMany, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var key in oneToMany)
            {
                var name = $"{key.DeclaringEntityType.ShortName()}.{key.DependentToPrincipal?.Name}";

                Assert.That(key.PrincipalToDependent, Is.Not.Null, $"{name} points at {key.PrincipalEntityType.ShortName()} and nothing points back");
            }
        }
    }

    /// <summary>
    /// From the same review: a navigation is followed because a query asked, never because it exists.
    /// </summary>
    [Test]
    public void NothingIsEagerLoaded()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var eager = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetNavigations())
            .Where(navigation => navigation.IsEagerLoaded)
            .Select(navigation => $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}")
            .ToList();

        Assert.That(eager, Is.Empty, "auto-include is off, and an AutoInclude() would turn one read of the case list into a read of everything under it");
    }

    [Test]
    public void AFileBelongsToItsPrimaryActAndKeepsTheNameItArrivedWith()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var asset = context.Model.FindEntityType(typeof(FileAsset));

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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        IReadOnlyEntityType?[] mapped = [context.Model.FindEntityType(typeof(FileAsset)), context.Model.FindEntityType(typeof(ActFileReference))];

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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var reference = context.Model.FindEntityType(typeof(ActFileReference));

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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var asset = context.Model.FindEntityType(typeof(FileAsset));
        var reference = context.Model.FindEntityType(typeof(ActFileReference));

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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var asset = context.Model.FindEntityType(typeof(FileAsset));

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
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        Type[] fileTypes = [typeof(FileAsset), typeof(ActFileReference)];
        var columns = fileTypes.SelectMany(type => ColumnsOf(context.Model.FindEntityType(type))).ToList();
        var kernel = typeof(PartyKind).Assembly.GetTypes().Select(type => type.Name).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Is.Not.Empty, "the file tables are mapped at all, or this test passes vacuously");
            Assert.That(Naming(columns, "Role"), Is.Empty, "a role column is back on a file");
            Assert.That(Naming(kernel, "FileRole"), Is.Empty, "a file role enum is back in the shared kernel");
        }
    }

    [Test]
    public void ANoteHangsOnACaseOrAnActAndTheDatabaseHoldsThat()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        // The read-optimized model drops check constraints; only the design-time one carries them.
        var comment = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        var check = comment.GetCheckConstraints().SingleOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(check, Is.Not.Null, "the rule is in the database, not only in the code that writes a note");
            Assert.That(check?.Sql, Does.Contain("<>"), "exactly one parent — never both, never neither");
            Assert.That(comment.FindProperty(nameof(Comment.CaseId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.ActId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.Body))?.GetMaxLength(), Is.Null, "a note is as long as it needs to be");
        }
    }

    [Test]
    public void ANoteGoesWithWhateverItHangsOn()
    {
        using var context = new ApplicationDbContextFactory().CreateDbContext([]);

        var comment = context.Model.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        Assert.That(
            comment.GetForeignKeys().All(key => key.DeleteBehavior == DeleteBehavior.Cascade),
            Is.True,
            "a note has no meaning without its case, its act or its author");
    }

    private static List<string> ColumnsOf(IReadOnlyEntityType? entityType) =>
        entityType?.GetProperties().Select(property => property.GetColumnName()).ToList() ?? [];

    private static List<string> Naming(IEnumerable<string> names, string word) =>
        [.. names.Where(name => name.Contains(word, StringComparison.OrdinalIgnoreCase))];

    private static IReadOnlyForeignKey? ForeignKeyTo<TPrincipal>(IReadOnlyEntityType entityType) =>
        entityType.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(TPrincipal));

    private static bool IsIndexed(IReadOnlyEntityType entityType, string propertyName) =>
        entityType.GetIndexes().Any(index => index.Properties.Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal)));
}
