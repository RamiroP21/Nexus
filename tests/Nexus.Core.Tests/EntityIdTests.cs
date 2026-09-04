namespace Nexus.Core.Tests;

public sealed class EntityIdTests
{
    [Fact]
    public void ZeroIsTheInvalidUnassignedIdentity()
    {
        Assert.Equal(default, EntityId.Invalid);
        Assert.Equal(0UL, EntityId.Invalid.Value);
        Assert.False(EntityId.Invalid.IsValid);
    }

    [Fact]
    public void NonZeroIdentityIsValidAndValueBased()
    {
        var identity = new EntityId(73);

        Assert.True(identity.IsValid);
        Assert.Equal(new EntityId(73), identity);
        Assert.NotEqual(new EntityId(74), identity);
        Assert.Equal("73", identity.ToString());
    }

    [Fact]
    public void IdentitiesHaveExplicitUnsignedOrdering()
    {
        var lower = new EntityId(1);
        var higher = new EntityId(ulong.MaxValue);

        Assert.True(lower < higher);
        Assert.True(lower <= higher);
        Assert.True(higher > lower);
        Assert.True(higher >= lower);
        Assert.True(lower.CompareTo(higher) < 0);
    }
}
