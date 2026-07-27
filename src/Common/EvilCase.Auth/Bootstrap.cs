using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EvilCase.Auth;

public static class Bootstrap
{
    private static string? settingsPath;

    public static WebApplicationBuilder AddEvilCaseAuth(this WebApplicationBuilder builder, string authSettingsPath)
    {
        settingsPath = authSettingsPath;

        var settings = builder.Configuration
            .GetRequiredSection(authSettingsPath)
            .Get<AuthSettings>(options => options.ErrorOnUnknownConfiguration = true)
            ?? throw new InvalidOperationException($"Missing {authSettingsPath} configuration settings");

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var key = Encoding.UTF8.GetBytes(settings.Jwt.Key);

                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidIssuer = settings.Jwt.Issuer;
                options.TokenValidationParameters.ValidAudience = settings.Jwt.Audience;
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(key);
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    public static IServiceCollection AddEvilCaseAuth(this IServiceCollection serviceCollection)
    {
        if (settingsPath is null)
            throw new InvalidOperationException("EvilCaseAuth was not configured. Call AddEvilCaseAuth on WebApplicationBuilder at startup.");

        serviceCollection.AddOptions<AuthSettings>()
            .BindConfiguration(settingsPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        serviceCollection.AddScoped<IAuthTokenService, AuthTokenService>();

        return serviceCollection;
    }
}
