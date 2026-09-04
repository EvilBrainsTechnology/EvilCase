namespace EvilBrains.EvilCase.Tests.Data;

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
