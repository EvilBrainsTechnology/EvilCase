using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.AI.Api.Controllers;

#pragma warning disable RCS1060

[ApiController]
[Route("test/inheritance")]
public class TestInheritanceController : Controller
{
    [HttpGet]
    public Entity[] Index()
    {
        try
        {
            return [
                new Entity(new IntIdentifier(25)),
                new Entity(new StringIdentifier("hello", "other value")),
                new Entity(new IntIdentifier(64)),
            ];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}

[JsonDerivedType(typeof(IntIdentifier), nameof(IntIdentifier))]
[JsonDerivedType(typeof(StringIdentifier), nameof(StringIdentifier))]
public abstract record EntityIdentifier(string Type);

public record IntIdentifier(int Id) : EntityIdentifier("Int");

public record StringIdentifier(string Id, string Value) : EntityIdentifier("String");

public record Entity(EntityIdentifier Id);
