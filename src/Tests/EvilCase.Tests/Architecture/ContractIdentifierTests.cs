using System.Reflection;

namespace EvilBrains.EvilCase.Tests.Architecture;

public class ContractIdentifierTests
{
    [Test]
    public void NoContractTypeCarriesABareId()
    {
        var offenders = typeof(Api.Contract.Cases.CaseListItem).Assembly
            .GetExportedTypes()
            .SelectMany(static type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Where(static property => string.Equals(property.Name, "Id", StringComparison.Ordinal))
            .Select(static property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.That(offenders, Is.Empty, "an identifier value on the contract names its entity, never a bare Id");
    }
}
