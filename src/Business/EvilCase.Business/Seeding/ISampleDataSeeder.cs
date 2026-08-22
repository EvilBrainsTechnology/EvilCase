namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    /// <summary>
    /// Writes the whole sample case tree into the tenant the given user belongs to, under that user.
    /// </summary>
    public Task Seed(Guid userId, CancellationToken cancellationToken);
}
