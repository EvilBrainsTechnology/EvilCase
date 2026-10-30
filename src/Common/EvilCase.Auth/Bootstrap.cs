using System.Text;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EvilBrains.EvilCase.Auth;

public static class Bootstrap
{
    private const string SelectorScheme = "EvilCase";

    public static IServiceCollection AddEvilCaseAuth(this IServiceCollection services)
    {
        services
            .AddOptions<AuthSettings>()
            .BindConfiguration("EvilBrains:EvilCase:Auth", static options => options.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<AuthSettings>, AuthSettingsValidator>();

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IUserStore, UserStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IUserSeeder, UserSeeder>();
        services.AddScoped<IApiTokenService, ApiTokenService>();
        services.AddScoped<IApiTokenStore, ApiTokenStore>();

        // An API token goes to its own handler, anything else to the JWT, which also reads the access
        // cookie; a token beside the cookie therefore wins.
        services
            .AddAuthentication(SelectorScheme)
            .AddPolicyScheme(
                SelectorScheme,
                displayName: null,
                static options => options.ForwardDefaultSelector = static context => ApiTokenValue.Read(context.Request) is null
                    ? JwtBearerDefaults.AuthenticationScheme
                    : ApiTokenAuthenticationHandler.SchemeName)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(ApiTokenAuthenticationHandler.SchemeName, configureOptions: null);

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthSettings>>(ConfigureJwtBearer);

        // Default deny: an unattributed endpoint fails closed; open ones say [AllowAnonymous].
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(
                ApiTokenPolicy.Name,
                static policy => policy
                    .AddAuthenticationSchemes(ApiTokenAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .RequireClaim(AuthClaims.ApiTokenId));

        return services;
    }

    /// <summary>
    /// Between authentication and authorization.
    /// </summary>
    public static IApplicationBuilder UseEvilCaseApiTokenBoundary(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiTokenBoundaryMiddleware>();
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IOptions<AuthSettings> authSettings)
    {
        var jwt = authSettings.Value.Jwt;

        // Off, or "sub" and "role" arrive under WS-Federation URIs.
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

        // The browser sends the token as a cookie; a caller with an Authorization header keeps using it.
        options.Events = new() { OnMessageReceived = ReadAccessCookie };
    }

    private static Task ReadAccessCookie(MessageReceivedContext context)
    {
        if (string.IsNullOrEmpty(context.Request.Headers.Authorization)
            && HostCookie.Read(context.Request, AccessCookie.Name) is { } token)
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    }

    public static async Task SeedEvilCaseUser(this IHost host, CancellationToken token)
    {
        await using var scope = host.Services.CreateAsyncScope();

        var seeder = scope.ServiceProvider.GetRequiredService<IUserSeeder>();

        // Before IDbSession is resolved: nothing to seed, no connection.
        if (!seeder.IsConfigured)
            return;

        var dbSession = scope.ServiceProvider.GetRequiredService<IDbSession>();
        await using var transaction = await dbSession.BeginTransaction(token);

        await seeder.SeedUser(token);

        await transaction.CommitAsync(token);
    }
}
