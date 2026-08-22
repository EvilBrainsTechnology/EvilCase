namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    /// <summary>
    /// Writes the whole sample case tree for the given tenant, owned by the given user. Opens its own
    /// transaction and its own <c>IUserContext</c> scope. The caller has already checked that the tenant
    /// holds no case.
    /// </summary>
    public Task Seed(Guid tenantId, Guid userId, CancellationToken cancellationToken);
}
