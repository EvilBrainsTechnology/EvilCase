using EvilBrains.EvilCase.Business;

namespace EvilBrains.EvilCase.Tests.Cases;

internal sealed class FakeOwnerContext : IOwnerContext
{
    public required long OwnerId { get; set; }

    public long? OwnerIdOrDefault => this.OwnerId;
}
