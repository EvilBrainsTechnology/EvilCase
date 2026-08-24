using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests;

/// <summary>
/// Takes the test container down once the whole run is over. NUnit instantiates it by concrete type.
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
