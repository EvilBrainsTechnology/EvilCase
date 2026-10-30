using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.Binding;
using EvilBrains.EvilCase.Api.Contract.Mcp;
using EvilBrains.EvilCase.Api.HealthChecks;
using EvilBrains.EvilCase.Api.Mcp;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Business;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.Logging.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using ModelContextProtocol.Protocol;
using Scalar.AspNetCore;

namespace EvilBrains.EvilCase.Api;

public static class Bootstrap
{
    // The name browser logs are recorded under; it identifies the deployment, not the library.
    private const string ClientSourceContext = "EvilBrains.EvilCase.App.Client";

    private const string BearerSecurityScheme = "Bearer";

    // IEnumerable, not an array: WithTools would take an array as the target instance of WithTools<T>.
    private static readonly IEnumerable<Type> McpToolTypes =
    [
        typeof(CaseTools),
        typeof(ActTools),
        typeof(ContactTools),
        typeof(LabelTools),
        typeof(CommentTools),
        typeof(FileTools),
        typeof(FileLinkTools),
        typeof(SignatureTools),
        typeof(SearchTools),
    ];

    // A RequestDelegate: a minimal-API handler would have to bind the unused {**path}.
    private static readonly RequestDelegate NotFound = static async context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        await problemDetails.TryWriteAsync(new()
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
        services
            .AddControllers(static options =>
            {
                // Ahead of the list binder, behind every binder a binding source picks.
                var collections = options.ModelBinderProviders.IndexOf(
                    options.ModelBinderProviders.First(static provider => provider is CollectionModelBinderProvider));

                options.ModelBinderProviders.Insert(collections, new ReadOnlyListModelBinderProvider());
            })
            .AddApplicationPart(typeof(Bootstrap).Assembly);

        // Backs the 404 fallback below; [ApiController] builds its own responses through ProblemDetailsFactory.
        services.AddProblemDetails();

        services.AddOpenApi(static options =>
        {
            options.AddDocumentTransformer(static async (document, _, _) =>
            {
                document.Info.Title = "EvilCase API";

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.Ordinal);
                document.Components.SecuritySchemes[BearerSecurityScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Description = "A personal API token (evc_…) created in Nastavení → API tokeny.",
                };

                document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerSecurityScheme, document)] = [] }];
            });
        });

        services.AddClientLogWriter(ClientSourceContext);

        services.AddEvilCaseBusiness();

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, PrincipalUserContext>();

        // Stateless: every request stands alone, so no session outlives a revoked token.
        services
            .AddMcpServer(static options => options.ServerInfo = new Implementation { Name = "EvilCase", Version = "1.0" })
            .WithHttpTransport(static options => options.Stateless = true)
            .WithTools(McpToolTypes)
            .WithRequestFilters(static filters => filters
                .AddListToolsFilter(McpScopeFilters.ListTools)
                .AddCallToolFilter(McpScopeFilters.CallTool));

        return services;
    }

    public static IHealthChecksBuilder AddEvilCaseApiHealthChecks(this IHealthChecksBuilder builder, params string[] tags)
    {
        builder.AddEvilCaseBusinessHealthChecks(tags);

        return builder;
    }

    public static IEndpointRouteBuilder MapEvilCaseApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();

        endpoints
            .MapMcp(McpRoute.Path)
            .RequireAuthorization(ApiTokenPolicy.Name);

        // Anonymous in every environment, so an integration reads the contract before it holds a token.
        endpoints
            .MapOpenApi()
            .AllowAnonymous();

        // Liveness runs no check: the process answering is the signal. A database check here would
        // restart every instance at once on a brief outage.
        endpoints
            .MapHealthChecks(HealthCheckPaths.Live, new HealthCheckOptions { Predicate = static _ => false })
            .AllowAnonymous();

        endpoints
            .MapHealthChecks(
                HealthCheckPaths.Ready,
                new HealthCheckOptions
                {
                    Predicate = static check => check.Tags.Contains(HealthCheckTags.Ready),
                    ResponseWriter = HealthCheckResponseWriter.Write,

                    // Degraded is 200 by default, which would keep an instance in rotation on a partial failure.
                    ResultStatusCodes = { [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable },
                })
            .AllowAnonymous();

        // The literal segment outranks the host's index.html fallback.
        endpoints
            .MapFallback("/api/{**path}", NotFound)
            .AllowAnonymous();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapEvilCaseApiReference(this IEndpointRouteBuilder endpoints)
    {
        // Anonymous, so mapped in Development only.
        endpoints
            .MapScalarApiReference()
            .AllowAnonymous();

        return endpoints;
    }
}
