using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EvilBrains.EvilCase.Auth;

public static class Bootstrap
{
    public static WebApplicationBuilder AddEvilCaseAuth(this WebApplicationBuilder builder, string authSettingsPath)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddOptions<AuthSettings>()
            .BindConfiguration(authSettingsPath, options => options.ErrorOnUnknownConfiguration = true)
            .ValidateOnStart();

        builder.Services.AddSingleton<IValidateOptions<AuthSettings>, AuthSettingsValidator>();

        builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configured through the options pipeline rather than from a second binding of the section, so
        // the validated settings are the only ones the scheme can be built from.
        builder.Services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthSettings>>((options, authSettings) =>
            {
                var jwt = authSettings.Value.Jwt;

                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ValidIssuer = jwt.Issuer;
                options.TokenValidationParameters.ValidAudience = jwt.Audience;
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
                options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;

                // Tokens are signed with HS256; without this a caller could pick the algorithm.
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
