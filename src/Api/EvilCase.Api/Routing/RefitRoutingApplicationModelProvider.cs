using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Refit;

namespace EvilBrains.EvilCase.Api.Routing;

/// <summary>
/// Derives MVC routes for controllers implementing Refit client interfaces,
/// making the Refit interface the single source of truth for route and HTTP method.
/// </summary>
internal sealed class RefitRoutingApplicationModelProvider : IApplicationModelProvider
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
                var attribute = FindRefitAttribute(controller.ControllerType, action.ActionMethod);
                if (attribute is null)
                    continue;

                var selector = action.Selectors.Single();
                selector.AttributeRouteModel = new(new RouteAttribute(attribute.Path));
                selector.ActionConstraints.Add(new HttpMethodActionConstraint([attribute.Method.Method]));
            }
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    { }

    private static HttpMethodAttribute? FindRefitAttribute(Type controllerType, MethodInfo actionMethod)
    {
        foreach (var interfaceType in controllerType.GetInterfaces())
        {
            var map = controllerType.GetInterfaceMap(interfaceType);
            var index = Array.IndexOf(map.TargetMethods, actionMethod);
            if (index < 0)
                continue;

            var attribute = map.InterfaceMethods[index].GetCustomAttribute<HttpMethodAttribute>();
            if (attribute is not null)
                return attribute;
        }

        return null;
    }
}
