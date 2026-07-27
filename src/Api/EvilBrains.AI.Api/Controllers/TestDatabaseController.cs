using EvilBrains.AI.Data.DbContexts;
using EvilBrains.AI.Data.Entities;
using EvilBrains.EntityFramework;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.AI.Api.Controllers;

#pragma warning disable RCS1060

[ApiController]
[Route("test/database")]
public class TestDatabaseController(ApplicationDbContext dbContext) : Controller
{
    [HttpGet]
    public async Task<IReadOnlyList<TestItem>> List()
    {
        return await dbContext.TestItems
            .OrderByDescending(x => x.Created)
            .AsReadOnlyListAsync();
    }

    [HttpPost]
    public async Task<TestItem> Add([FromQuery] string text)
    {
        var testItem = new TestItem
        {
            Created = DateTime.UtcNow,
            Text = text,
        };

        await dbContext.AddAsync(testItem);
        await dbContext.SaveChangesAsync();

        return testItem;
    }
}
