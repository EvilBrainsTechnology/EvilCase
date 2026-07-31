using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Events;

namespace EvilBrains.EvilCase.Api.HealthChecks;

public static class HealthCheckBootstrap
{
    private const string PathPrefix = "/healthz";

    private const string LivePath = PathPrefix + "/live";

    private const string ReadyPath = PathPrefix + "/ready";

    private const string LiveTag = "live";

    public static IServiceCollection AddEvilCaseHealthChecks(this IServiceCollection services)
    {
        // Only the "live" tag is opt-in. Every other check belongs to readiness, so a new check
        // cannot silently fall out of the readiness gate by forgetting a tag.
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag])
            .AddApplicationLifecycleHealthCheck()
            .AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    public static WebApplication MapEvilCaseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(LivePath, CreateOptions(app.Environment, registration => registration.Tags.Contains(LiveTag)))
            .ExcludeFromDescription()
            .ShortCircuit();

        app.MapHealthChecks(ReadyPath, CreateOptions(app.Environment))
            .ExcludeFromDescription()
            .ShortCircuit();

        return app;
    }

    public static LogEventLevel GetRequestLogLevel(HttpContext context, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        return context.Request.Path.StartsWithSegments(PathPrefix, StringComparison.Ordinal)
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    }

    private static HealthCheckOptions CreateOptions(IHostEnvironment environment, Func<HealthCheckRegistration, bool>? predicate = null)
    {
        var options = new HealthCheckOptions { Predicate = predicate };

        // The default writer returns the bare status. A detailed entry carries the description of a
        // failed check, which for the database check is the raw exception message: host, port,
        // database name and the reason authentication failed.
        if (environment.IsDevelopment())
            options.ResponseWriter = HealthCheckResponseWriter.WriteAsync;

        return options;
    }
}
