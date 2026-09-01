namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// A tenant seeded before every test and disposed after it (SDD-006). A fixture that wires the
/// context the way the host does overrides <see cref="AsHost"/>; one with more setup adds its own
/// <c>[SetUp]</c>/<c>[TearDown]</c>, which NUnit runs after/before this one's.
/// </summary>
public abstract class TenantFixture
{
    private protected TestTenant Tenant { get; private set; } = null!;

    protected virtual bool AsHost => false;

    [SetUp]
    public async Task SetUpTenant()
    {
        this.Tenant = await TestTenant.Create(this.AsHost);
    }

    [TearDown]
    public async Task TearDownTenant()
    {
        await this.Tenant.DisposeAsync();
    }
}
