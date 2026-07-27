using EvilBrains.Cryptography;
using EvilCase.Auth;
using EvilCase.Data.DbContexts;
using EvilCase.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvilCase.Api.Controllers;

#pragma warning disable RCS1060

[ApiController]
[Route("auth")]
public class AuthController(ApplicationDbContext dbContext) : Controller
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Created = DateTime.UtcNow,
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return this.Created();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromServices] IAuthTokenService authTokenService,
        [FromBody] LoginRequest request)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == request.Email);
        if (user is null)
        {
            PasswordHasher.FakeVerify();
            return this.NotFound();
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return this.NotFound();

        var token = authTokenService.GenerateToken(user);
        var loginResponseModel = new LoginResponse
        {
            Email = user.Email,
            Token = token,
        };

        return this.Ok(loginResponseModel);
    }

    [HttpGet("user-info")]
    [Authorize]
    public UserInfo UserInfo()
    {
        var userInfo = new UserInfo { Email = this.User!.Identity!.Name! };
        return userInfo;
    }
}

public record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}

public record LoginRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}

public record LoginResponse
{
    public required string Email { get; init; }

    public required string Token { get; init; }
}

public record UserInfo
{
    public required string Email { get; init; }
}

#pragma warning restore RCS1060
