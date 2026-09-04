namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    public Task SeedSampleData(Guid tenantId, Guid userId, CancellationToken token);
}
