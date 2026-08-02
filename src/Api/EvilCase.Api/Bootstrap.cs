using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Data;
using EvilBrains.Logging.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace EvilBrains.EvilCase.Api;

public static class Bootstrap
{
    // The name browser logs are recorded under; it identifies the deployment, not the library.
    private const string ClientSourceContext = "EvilBrains.EvilCase.App.Client";

    // A RequestDelegate rather than a minimal API handler: the catch-all segment only exists to give
    // this fallback a literal prefix, and binding it would leave an unused parameter behind. The body is
    // written through the problem details service so an unknown path answers in the same shape as every
    // other API error rather than with nothing at all.
    private static readonly RequestDelegate NotFound = static async context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        _ = await problemDetails.TryWriteAsync(new()
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = "No API endpoint matches the requested path.",
            },
        });
    };

    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // The controllers live in this library rather than in the host, so the part is added explicitly.
        services.AddControllers().AddApplicationPart(typeof(Bootstrap).Assembly);

        // Backs the 404 fallback below; [ApiController] builds its own responses through ProblemDetailsFactory.
        services.AddProblemDetails();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "EvilCase API";
                return Task.CompletedTask;
            });
        });

        services.AddClientLogWriter(ClientSourceContext);

        services.AddEvilCaseData();

        // The one place ownership is resolved. Scoped: it answers for the request being served.
        services.AddHttpContextAccessor();
        services.AddScoped<IOwnerContext, PrincipalOwnerContext>();

        services
            .AddHealthChecks()
            .AddEvilCaseDataHealthChecks(HealthCheckTags.Ready);

        return services;
    }

    public static IEndpointRouteBuilder MapEvilCaseApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();

        // Liveness runs no check: the process answering is the signal. A database check here would
        // restart every instance at once on a brief outage.
        endpoints.MapHealthChecks(HealthCheckPaths.Live, new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        endpoints.MapHealthChecks(
            HealthCheckPaths.Ready,
            new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
                ResponseWriter = HealthCheckResponseWriter.Write,

                // Degraded is 200 by default, which would keep an instance in rotation on a partial failure.
                ResultStatusCodes = { [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable },
            })
            .AllowAnonymous();

        // An unknown API path is a 404, never the host's index.html. The literal segment gives this
        // fallback precedence over the catch-all one serving the frontend. Anonymous, or the default
        // deny policy would answer an unknown path with 401 and tell a caller nothing at all.
        endpoints.MapFallback("/api/{**path}", NotFound).AllowAnonymous();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapEvilCaseApiReference(this IEndpointRouteBuilder endpoints)
    {
        // Development only, and the default deny policy would otherwise put the reference behind a token
        // the reference itself is the way to obtain.
        endpoints.MapOpenApi().AllowAnonymous();
        endpoints.MapScalarApiReference().AllowAnonymous();

        return endpoints;
    }
}
