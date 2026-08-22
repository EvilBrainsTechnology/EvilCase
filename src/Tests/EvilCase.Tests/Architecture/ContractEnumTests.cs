using System.Reflection;
using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Tests.Architecture;

/// <summary>
/// A wire enum without a string converter is serialized as a number, and reordering its members
/// silently changes the API.
/// </summary>
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
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .SelectMany(property => Unwrap(property.PropertyType))
                .Where(type => type.IsEnum)
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
