using System.Text;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EvilBrains.EvilCase.Auth;

// IOptions (not IOptionsSnapshot) on purpose: JwtBearer validation parameters are baked
// once at startup, so token generation must use the same startup snapshot — otherwise
// a config reload would sign tokens the validation side rejects.
internal sealed class AuthTokenService(IOptions<AuthSettings> options, TimeProvider timeProvider) : IAuthTokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken Generate(User user, Guid authSessionId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwt = options.Value.Jwt;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.Add(jwt.AccessTokenExpiration);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [AuthClaims.Subject] = user.Id.ToString(CultureInfo.InvariantCulture),
                [AuthClaims.Email] = user.Email,
                [AuthClaims.Role] = user.Role.ToString(),
                [AuthClaims.AuthSessionId] = authSessionId.ToString("N", CultureInfo.InvariantCulture),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            },
        };

        return new() { Value = TokenHandler.CreateToken(descriptor), ExpiresAt = expires };
    }
}
