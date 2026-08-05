using EvilBrains.EvilCase.Business;

namespace EvilBrains.EvilCase.Tests.Numbering;

internal sealed class FixedOwnerContext(long ownerId) : IOwnerContext
{
    public long OwnerId => ownerId;

    public long? OwnerIdOrDefault => ownerId;
}
