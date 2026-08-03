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

    private static readonly Assembly Contract = typeof(Api.Contract.Echo.EchoRequest).Assembly;

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
        }
    }

    private static bool References(Assembly assembly, Assembly referenced) => assembly
        .GetReferencedAssemblies()
        .Any(reference => string.Equals(reference.Name, referenced.GetName().Name, StringComparison.Ordinal));
}
