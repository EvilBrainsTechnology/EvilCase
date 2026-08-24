using System.Text;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EvilBrains.EvilCase.Auth;

public static class Bootstrap
{
    public static IServiceCollection AddEvilCaseAuth(this IServiceCollection services, string authSettingsPath)
    {
        services
            .AddOptions<AuthSettings>()
            .BindConfiguration(authSettingsPath, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AuthSettings>, AuthSettingsValidator>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IUserSeeder, UserSeeder>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configured through the options pipeline rather than from a second binding of the section, so
        // the validated settings are the only ones the scheme can be built from.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthSettings>>((options, authSettings) =>
            {
                var jwt = authSettings.Value.Jwt;

                // Claims stay as the token wrote them. Mapped, "sub" and "role" would arrive under
                // WS-Federation URIs and everything reading them would have to name those instead.
                options.MapInboundClaims = false;

                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidIssuer = jwt.Issuer;
                options.TokenValidationParameters.ValidAudience = jwt.Audience;
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;

                // Without these the principal has no name and no roles at all, because mapping is off.
                options.TokenValidationParameters.NameClaimType = AuthClaims.Email;
                options.TokenValidationParameters.RoleClaimType = AuthClaims.Role;

                // Tokens are signed with HS256; without this a caller could pick the algorithm.
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
            });

        // Default deny. An endpoint that carries no authorization attribute now requires an authenticated
        // caller, and everything meant to stay open — the health probes, the sign-in endpoints, the
        // frontend itself — says so with [AllowAnonymous]. Adding an endpoint and forgetting to protect
        // it fails closed rather than open.
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    /// <summary>
    /// Creates the account, the tenant, the configured administrator and the administrator's default
    /// contact where the database holds no user at all. Runs after the migrations and before anything
    /// is served, so an empty deployment is reachable on first start.
    /// </summary>
    public static async Task SeedEvilCaseUser(this IHost host, CancellationToken cancellationToken)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IUserSeeder>();

        // A deployment that names no seed never reaches the database, so a host with nothing to seed
        // starts without a connection.
        if (!seeder.IsConfigured)
            return;

        var dbSession = scope.ServiceProvider.GetRequiredService<IDbSession>();
        await using var transaction = await dbSession.BeginTransaction(cancellationToken);

        await seeder.SeedUser(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
