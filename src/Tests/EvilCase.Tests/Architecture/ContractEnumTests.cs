using System.Reflection;
using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Tests.Architecture;

public class ContractEnumTests
{
    [Test]
    public void EveryEnumOnTheWireIsSerializedByName()
    {
        var enums = WireEnums();

        Assert.That(enums, Is.Not.Empty, "the contract carries enums at all, or this test passes vacuously");

        using (Assert.EnterMultipleScope())
        {
            foreach (var type in enums)
            {
                Assert.That(
                    type.GetCustomAttribute<JsonConverterAttribute>(),
                    Is.Not.Null,
                    $"{type.Name}: an enum the contract names is serialized by name");
            }
        }
    }

    private static IReadOnlyList<Type> WireEnums()
    {
        return
        [
            .. typeof(Api.Contract.Cases.CaseListItem).Assembly
                .GetExportedTypes()
                .SelectMany(static type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .SelectMany(static property => Unwrap(property.PropertyType))
                .Where(static type => type.IsEnum)
                .Distinct(),
        ];
    }

    private static IEnumerable<Type> Unwrap(Type type)
    {
        yield return Nullable.GetUnderlyingType(type) ?? type;

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var unwrapped in Unwrap(argument))
                yield return unwrapped;
        }
    }
}
