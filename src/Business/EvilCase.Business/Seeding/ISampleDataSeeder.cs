namespace EvilBrains.EvilCase.Business.Seeding;

internal interface ISampleDataSeeder
{
    /// <summary>
    /// Writes the whole sample case tree into the tenant the caller has entered, owned by the given user.
    /// The caller has already checked that the tenant holds no case.
    /// </summary>
    public Task Seed(Guid userId, CancellationToken cancellationToken);
}
