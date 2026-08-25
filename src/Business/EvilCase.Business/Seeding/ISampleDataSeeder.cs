namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    /// <summary>
    /// Writes the whole sample case tree into the given tenant, under the given user.
    /// </summary>
    public Task SeedSampleData(Guid tenantId, Guid userId, CancellationToken token);
}
