using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The recursive walk against a server. <c>ToQueryString</c> parses no SQL and the endpoint tests stand the
/// reader in, so nothing else would see a syntax error in the CTE or a cycle that never ends.
/// </summary>
public class CaseWalkDatabaseTests
{
    /// <summary>
    /// Far past what any JSON serializer's default depth allows, and past the distance the walk used to
    /// stop at.
    /// </summary>
    private const int ChainLength = 200;

    private ApplicationDbContext? context;

    private long owner;

    private long deepest;

    private long cycled;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        this.context = TestDatabase.Create();

        if (this.context is null)
            return;

        this.owner = await this.SeedOwner();
        this.deepest = await this.SeedChain();
        this.cycled = await this.SeedCycle();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (this.context is null)
            return;

        await this.context.Database.EnsureDeletedAsync();
        await this.context.DisposeAsync();
    }

    [SetUp]
    public void RequireDatabase()
    {
        if (this.context is null)
            Assert.Ignore("no PostgreSQL answered on EVILCASE_TESTS_POSTGRES, so the walk cannot be run against a server");
    }

    /// <summary>
    /// The SQL only ever reaches a parser here.
    /// </summary>
    [Test]
    public async Task TheWalkRunsAgainstAServer()
    {
        var nodes = await this.Walk(this.deepest);

        Assert.That(nodes, Has.Count.EqualTo(ChainLength), "the walk reads every generation of the chain");
    }

    [Test]
    public async Task ANestingDeeperThanAnySerializerAllowsReachesItsRoot()
    {
        var nodes = await this.Walk(this.deepest);

        var ancestors = CaseGraph.Ancestors(nodes, this.deepest);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ancestors, Has.Count.EqualTo(ChainLength - 1), "no distance bounds how deep a case may nest");
            Assert.That(ancestors[0].CaseNumber, Is.EqualTo(Number(1)), "the path reads root first, so it starts at the root");
        }
    }

    [Test]
    public async Task TheWholeChainReadsBackAsSubCasesOfItsRoot()
    {
        var root = await this.context!.Cases.Where(@case => @case.CaseNumber == Number(1)).Select(@case => @case.Id).SingleAsync();

        var subCases = CaseGraph.SubCases(await this.Walk(root), root);

        Assert.That(subCases, Has.Count.EqualTo(ChainLength - 1), "every generation below the root is a sub-case of it");
    }

    /// <summary>
    /// Nothing can write a cycle today; the walk survives one rather than running forever.
    /// </summary>
    [Test]
    public async Task ACycleEndsTheWalkAndStillBuildsAGraph()
    {
        var nodes = await this.Walk(this.cycled);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nodes, Is.Not.Empty, "the walk ends instead of running forever");
            Assert.That(CaseGraph.Ancestors(nodes, this.cycled), Is.Not.Empty, "a repeated row does not stop the graph being built");
            Assert.That(CaseGraph.SubCases(nodes, this.cycled), Is.Not.Empty, "a repeated row does not stop the graph being built");
        }
    }

    private static string Number(int generation) => string.Create(CultureInfo.InvariantCulture, $"EC-CHAIN-{generation:D4}");

    private async Task<IReadOnlyList<CaseGraphNode>> Walk(long id) =>
        await this.context!.Cases.AroundCase(id).AsGraphNodes().ToListAsync();

    private async Task<long> SeedOwner()
    {
        var user = new User
        {
            Email = "walk@evilcase.test",
            PasswordHash = "not-a-hash",
            Role = UserRole.Admin,
            Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        _ = this.context!.Users.Add(user);
        _ = await this.context.SaveChangesAsync();

        return user.Id;
    }

    private async Task<long> SeedChain()
    {
        long? parent = null;

        for (var generation = 1; generation <= ChainLength; generation++)
        {
            var @case = this.Case(Number(generation), parent);

            _ = this.context!.Cases.Add(@case);
            _ = await this.context.SaveChangesAsync();

            parent = @case.Id;
        }

        return parent!.Value;
    }

    /// <summary>
    /// Three cases whose parent chain closes on itself, which the schema allows and no endpoint writes.
    /// </summary>
    private async Task<long> SeedCycle()
    {
        var first = this.Case("EC-CYCLE-1", parent: null);
        var second = this.Case("EC-CYCLE-2", parent: null);
        var third = this.Case("EC-CYCLE-3", parent: null);

        this.context!.Cases.AddRange(first, second, third);
        _ = await this.context.SaveChangesAsync();

        _ = await this.context.Database.ExecuteSqlAsync(
            $"""
             UPDATE "Cases" SET "ParentCaseId" = {second.Id} WHERE "Id" = {first.Id};
             UPDATE "Cases" SET "ParentCaseId" = {third.Id} WHERE "Id" = {second.Id};
             UPDATE "Cases" SET "ParentCaseId" = {first.Id} WHERE "Id" = {third.Id};
             """);

        return second.Id;
    }

    private Case Case(string caseNumber, long? parent) => new()
    {
        OwnerId = this.owner,
        ParentCaseId = parent,
        CaseNumber = caseNumber,
        Title = caseNumber,
        Status = CaseStatus.Active,
        Created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };
}
