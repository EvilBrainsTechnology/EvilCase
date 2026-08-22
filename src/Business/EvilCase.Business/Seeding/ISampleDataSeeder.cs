namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    /// <summary>
    /// Writes the whole sample case tree for the given tenant.
    /// </summary>
    public Task Seed(Guid userId, CancellationToken cancellationToken);
}
