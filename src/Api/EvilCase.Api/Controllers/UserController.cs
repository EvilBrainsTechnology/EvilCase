using EvilBrains.Cryptography;
using EvilBrains.EntityFramework;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Api.Controllers;

#pragma warning disable RCS1060

[ApiController]
[Route("users")]
public class UserController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IReadOnlyList<User>> List(CancellationToken token)
    {
        return await dbContext.Users
            .OrderByDescending(x => x.Created)
            .AsReadOnlyListAsync(token);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] UserModel userModel)
    {
        var user = new User
        {
            Email = userModel.Email,
            PasswordHash = PasswordHasher.Hash(userModel.Password),
            Created = DateTime.UtcNow,
        };

        await dbContext.Users.AddAsync(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return this.Conflict("Email is already registered");
        }

        return user;
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] UserModel userModel)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == userModel.Email);
        if (user is null)
            return this.NotFound("User not found");

        if (!PasswordHasher.Verify(userModel.Password, user.PasswordHash))
            return this.Unauthorized("Wrong password");

        return this.Ok("User verified successfully");
    }
}

public record UserModel
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}

#pragma warning restore RCS1060
