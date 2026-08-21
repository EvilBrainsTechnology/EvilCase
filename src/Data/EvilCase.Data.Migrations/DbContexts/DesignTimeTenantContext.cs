using EvilBrains.EvilCase.Domain.Tenancy;

namespace EvilBrains.EvilCase.Data.Migrations.DbContexts;

/// <summary>
/// Design time builds the model and never queries. A fixed tenant keeps the SQL the tests read stable.
/// </summary>
internal sealed class DesignTimeTenantContext : ITenantContext
{
    private static readonly Guid DesignTimeTenant = Guid.Parse("0195f000-0000-7000-8000-000000000001", CultureInfo.InvariantCulture);

    public Guid TenantId => DesignTimeTenant;

    public Guid? TenantIdOrDefault => DesignTimeTenant;

    public IDisposable Enter(Guid tenantId) => throw new NotSupportedException("Design time runs no work inside a tenant.");
}
