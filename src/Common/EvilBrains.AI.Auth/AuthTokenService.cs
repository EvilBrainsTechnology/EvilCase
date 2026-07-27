using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EvilBrains.AI.Data.Entities;
using EvilBrains.Collections.Factories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EvilBrains.AI.Auth;

internal sealed class AuthTokenService(IOptionsSnapshot<AuthSettings> options) : IAuthTokenService
{
    private static readonly JwtSecurityTokenHandler JwtSecurityTokenHandler = new();

    public string GenerateToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.Jwt.Key);
        var securityKey = new SymmetricSecurityKey(key);
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = ReadOnlyList.From(new Claim(JwtRegisteredClaimNames.UniqueName, user.Email));

        var token = new JwtSecurityToken(
            issuer: options.Value.Jwt.Issuer,
            audience: options.Value.Jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(options.Value.Jwt.TokenExpiration),
            signingCredentials: signingCredentials);

        return JwtSecurityTokenHandler.WriteToken(token);
    }
}
