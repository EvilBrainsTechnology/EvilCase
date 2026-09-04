using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests;

/// <summary>
/// NUnit instantiates it by concrete type.
/// </summary>
[SetUpFixture]
public sealed class TestDatabaseTeardown
{
    [OneTimeTearDown]
    public async Task RemoveDatabase()
    {
        await TestDatabase.Remove();
    }
}
