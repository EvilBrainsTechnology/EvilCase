using System.Reflection;
using EvilBrains.EvilCase.Business;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Architecture;

/// <summary>
/// The layering is a compiler fact, not a convention: an assembly reference exists only where a type
/// of it is actually used, so a controller that reaches for a DbContext fails here.
/// </summary>
public class LayerTests
{
    private static readonly Assembly Api = typeof(Api.Bootstrap).Assembly;

    private static readonly Assembly Business = typeof(Bootstrap).Assembly;

    private static readonly Assembly Data = typeof(ApplicationDbContext).Assembly;

    private static readonly Assembly Domain = typeof(CaseStatus).Assembly;

    private static readonly Assembly Contract = typeof(Api.Contract.Cases.CaseListItem).Assembly;

    private static readonly Assembly Client = typeof(Api.Client.Bootstrap).Assembly;

    private static readonly Assembly App = typeof(App.Icons.AppIcons).Assembly;

    private static readonly Assembly Files = typeof(EvilCase.Files.Bootstrap).Assembly;

    [Test]
    public void TheApiReachesTheDatabaseThroughBusiness()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(Api, Business), "the API has to go through the business layer");
            Assert.That(References(Api, Data), Is.False, "a controller must not touch the DbContext");
        }
    }

    [Test]
    public void PersistenceDoesNotKnowTheWireFormat()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(Data, Contract), Is.False, "an entity must not project into a wire DTO");
            Assert.That(References(Data, Business), Is.False, "the dependency runs business to data");
            Assert.That(References(Data, Domain), "entities speak the domain's vocabulary");
        }
    }

    [Test]
    public void TheDomainDependsOnNothingOfOurs()
    {
        var ours = Domain.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("EvilBrains.", StringComparison.Ordinal) == true)
            .Select(reference => reference.Name)
            .ToList();

        Assert.That(ours, Is.Empty, "the domain is the shared kernel and references nothing");
    }

    [Test]
    public void TheContractCarriesNoLogic()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(Contract, Data), Is.False, "the contract ships to the browser");
            Assert.That(References(Contract, Business), Is.False, "the contract ships to the browser");
            Assert.That(References(Contract, Domain), "a wire DTO and an entity name a status with the same enum");
        }
    }

    [Test]
    public void TheFrontendReachesTheApiOverHttp()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(App, Client), "the frontend calls the API through the generated client");
            Assert.That(References(App, Api), Is.False, "the browser is handed a wire contract, never the API itself");
            Assert.That(References(App, Business), Is.False, "the frontend renders and collects input, it never decides");
            Assert.That(References(App, Data), Is.False, "the schema never ships to the browser");
        }
    }

    [Test]
    public void TheGeneratedClientKnowsNothingButTheContract()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(Client, Contract), "a generated call takes and returns the shared DTOs");
            Assert.That(References(Client, Api), Is.False, "the client compiles the controller sources rather than referencing them");
            Assert.That(References(Client, Business), Is.False, "the client is HTTP and nothing else");
            Assert.That(References(Client, Data), Is.False, "the client is HTTP and nothing else");
        }
    }

    [Test]
    public void TheFileStoreKnowsNothingButBytes()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(References(Files, Data), Is.False, "the blob store is a closed module behind IFileBlobStore");
            Assert.That(References(Files, Business), Is.False, "the blob store is a closed module behind IFileBlobStore");
            Assert.That(References(Files, Contract), Is.False, "the blob store is a closed module behind IFileBlobStore");
            Assert.That(References(Files, Api), Is.False, "the blob store is a closed module behind IFileBlobStore");
        }
    }

    private static bool References(Assembly assembly, Assembly referenced) => assembly
        .GetReferencedAssemblies()
        .Any(reference => string.Equals(reference.Name, referenced.GetName().Name, StringComparison.Ordinal));
}
