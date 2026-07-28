using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace EvilBrains.EvilCase.Tests.Routing;

internal sealed class TypesApplicationPart(params Type[] types) : ApplicationPart, IApplicationPartTypeProvider
{
    public override string Name => nameof(TypesApplicationPart);

    public IEnumerable<TypeInfo> Types { get; } = [.. types.Select(x => x.GetTypeInfo())];
}
