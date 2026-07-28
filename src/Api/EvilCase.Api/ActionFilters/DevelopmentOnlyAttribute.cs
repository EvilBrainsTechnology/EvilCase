using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EvilBrains.EvilCase.Api.ActionFilters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class DevelopmentOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
            context.Result = new NotFoundResult();
    }
}
