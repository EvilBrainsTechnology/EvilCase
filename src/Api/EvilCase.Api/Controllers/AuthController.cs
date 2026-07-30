using EvilBrains.ApiClient;
using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("auth")]
public class AuthController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request, CancellationToken token)
    {
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Created = DateTime.UtcNow,
        };

        await dbContext.Users.AddAsync(user, token);

        try
        {
            await dbContext.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return this.Conflict("Email is already registered");
        }

        return this.Created();
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromServices] IAuthTokenService authTokenService,
        [FromBody] LoginRequest request,
        CancellationToken token)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == request.Email, token);
        if (user is null)
        {
            PasswordHasher.FakeVerify();
            return this.NotFound();
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            return this.NotFound();

        var authToken = authTokenService.GenerateToken(user);
        var loginResponseModel = new LoginResponse
        {
            Email = user.Email,
            Token = authToken,
        };

        return this.Ok(loginResponseModel);
    }

    [HttpGet("user_info")]
    [Authorize]
    public UserInfo UserInfo()
    {
        var email = this.User.Identity?.Name
            ?? throw new InvalidOperationException("Authenticated user is missing the name claim");

        return new() { Email = email };
    }
}
