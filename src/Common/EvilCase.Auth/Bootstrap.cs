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

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthSettings>>(static (options, authSettings) =>
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
            });

        // Default deny: an unattributed endpoint fails closed; open ones say [AllowAnonymous].
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
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
