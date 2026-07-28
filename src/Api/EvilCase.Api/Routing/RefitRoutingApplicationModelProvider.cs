using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Refit;

namespace EvilBrains.EvilCase.Api.Routing;

/// <summary>
/// Derives MVC routes for controllers implementing Refit client interfaces,
/// making the Refit interface the single source of truth for route and HTTP method.
/// Contracts whose binding semantics would not reach MVC (Refit parameter attributes,
/// route placeholders without a matching parameter) are rejected; extend the
/// translation here when an endpoint first needs them.
/// </summary>
internal sealed partial class RefitRoutingApplicationModelProvider : IApplicationModelProvider
{
    // Must run after DefaultApplicationModelProvider (-1000) and before
    // ApiBehaviorApplicationModelProvider (-900), which requires attribute routing.
    public int Order => -950;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers)
        {
            foreach (var action in controller.Actions)
            {
                var interfaceMethod = FindRefitMethod(controller.ControllerType, action.ActionMethod);
                var attribute = interfaceMethod?.GetCustomAttribute<HttpMethodAttribute>();
                if (interfaceMethod is null || attribute is null)
                    continue;

                ValidateContract(interfaceMethod, attribute);

                var selector = action.Selectors.Single();
                selector.AttributeRouteModel = new(new RouteAttribute(attribute.Path));
                selector.ActionConstraints.Add(new HttpMethodActionConstraint([attribute.Method.Method]));
            }
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    { }

    private static MethodInfo? FindRefitMethod(Type controllerType, MethodInfo actionMethod)
    {
        foreach (var interfaceType in controllerType.GetInterfaces())
        {
            var map = controllerType.GetInterfaceMap(interfaceType);
            var index = Array.IndexOf(map.TargetMethods, actionMethod);
            if (index < 0)
                continue;

            var interfaceMethod = map.InterfaceMethods[index];
            if (Attribute.IsDefined(interfaceMethod, typeof(HttpMethodAttribute)))
                return interfaceMethod;
        }

        return null;
    }

    private static void ValidateContract(MethodInfo method, HttpMethodAttribute attribute)
    {
        var parameters = method.GetParameters();

        foreach (var parameter in parameters)
        {
            var refitAttribute = parameter.GetCustomAttributes().FirstOrDefault(x => string.Equals(x.GetType().Namespace, nameof(Refit), StringComparison.Ordinal));
            if (refitAttribute is not null)
            {
                throw new InvalidOperationException(
                    $"{method.DeclaringType}.{method.Name}: parameter '{parameter.Name}' uses [{refitAttribute.GetType().Name}], which interface routing does not translate to MVC binding. Extend {nameof(RefitRoutingApplicationModelProvider)} or drop the attribute.");
            }
        }

        foreach (Match placeholder in RoutePlaceholderRegex.Matches(attribute.Path))
        {
            var name = placeholder.Groups["name"].Value;
            if (!parameters.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"{method.DeclaringType}.{method.Name}: route placeholder '{{{name}}}' has no matching method parameter.");
            }
        }
    }

    [GeneratedRegex(@"\{(?<name>\w+)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex RoutePlaceholderRegex { get; }
}
