using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Data.Migrations.DbContexts;

/// <summary>
/// Design time builds the model and never queries. A fixed tenant and user keep the SQL the tests read stable.
/// </summary>
internal sealed class DesignTimeUserContext : IUserContext
{
    private static readonly Guid DesignTimeTenant = Guid.Parse("0195f000-0000-7000-8000-000000000001", CultureInfo.InvariantCulture);

    private static readonly Guid DesignTimeUser = Guid.Parse("0195f000-0000-7000-8000-000000000002", CultureInfo.InvariantCulture);

    public Guid TenantId => DesignTimeTenant;

    public Guid? TenantIdOrDefault => DesignTimeTenant;

    public Guid UserId => DesignTimeUser;

    public IDisposable Enter(Guid tenantId, Guid userId)
    {
        throw new NotSupportedException("Design time runs no work inside a tenant.");
    }
}
