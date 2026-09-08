using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class EntityRegistryTests
{
    [Fact]
    public void GenerationIdentityPreservesTheOriginalUnsignedValueProtocol()
    {
        EntityId entity = EntityId.FromParts(73, 12);

        Assert.Equal(73U, entity.Index);
        Assert.Equal(12U, entity.Generation);
        Assert.Equal((12UL << 32) | 73UL, entity.Value);
        Assert.Equal(entity, new EntityId(entity.Value));
        var rawHasher = new StableHasher64();
        var partsHasher = new StableHasher64();
        rawHasher.Add(new EntityId(entity.Value));
        partsHasher.Add(entity);
        Assert.Equal(rawHasher.Value, partsHasher.Value);
    }

    [Theory]
    [InlineData(0U, 1U)]
    [InlineData(1U, 0U)]
    public void GenerationIdentityRejectsUnassignedParts(uint index, uint generation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EntityId.FromParts(index, generation));
    }

    [Fact]
    public void CreationUsesAscendingSlotsAndStartsAtGenerationOne()
    {
        var registry = new EntityRegistry();

        Assert.Equal(EntityId.FromParts(1, 1), registry.CreateEntity());
        Assert.Equal(EntityId.FromParts(2, 1), registry.CreateEntity());
        Assert.Equal(2, registry.AliveCount);
        Assert.Equal(2, registry.AllocatedSlotCount);
    }

    [Fact]
    public void SlotArraysGrowWithoutChangingExistingIdentities()
    {
        var registry = new EntityRegistry(1);
        for (uint index = 1; index <= 257; index++)
        {
            Assert.Equal(EntityId.FromParts(index, 1), registry.CreateEntity());
        }

        for (uint index = 1; index <= 257; index++)
        {
            Assert.True(registry.IsAlive(EntityId.FromParts(index, 1)));
        }

        Assert.Equal(257, registry.AliveCount);
        Assert.Equal(257, registry.AllocatedSlotCount);
    }

    [Fact]
    public void AliveChecksRejectInvalidUnallocatedAndWrongGenerationIdentities()
    {
        var registry = new EntityRegistry();
        EntityId entity = registry.CreateEntity();

        Assert.True(registry.IsAlive(entity));
        Assert.False(registry.IsAlive(EntityId.Invalid));
        Assert.False(registry.IsAlive(new EntityId(1)));
        Assert.False(registry.IsAlive(new EntityId(1UL << 32)));
        Assert.False(registry.IsAlive(EntityId.FromParts(2, 1)));
        Assert.False(registry.IsAlive(EntityId.FromParts(1, 2)));
        Assert.False(registry.IsAlive(new EntityId(ulong.MaxValue)));
    }

    [Fact]
    public void DestroyAndSlotReuseKeepObsoleteReferencesDead()
    {
        var registry = new EntityRegistry();
        EntityId original = registry.CreateEntity();

        Assert.True(registry.DestroyEntity(original));
        Assert.False(registry.IsAlive(original));
        Assert.Equal(0, registry.AliveCount);
        EntityId replacement = registry.CreateEntity();

        Assert.Equal(original.Index, replacement.Index);
        Assert.Equal(original.Generation + 1, replacement.Generation);
        Assert.NotEqual(original, replacement);
        Assert.True(registry.IsAlive(replacement));
        Assert.False(registry.IsAlive(original));
        Assert.False(registry.DestroyEntity(original));
        Assert.True(registry.IsAlive(replacement));
        Assert.Equal(1, registry.AliveCount);
        Assert.Equal(1, registry.AllocatedSlotCount);
    }

    [Fact]
    public void DoubleDestroyDoesNotDuplicateAFreeSlot()
    {
        var registry = new EntityRegistry();
        EntityId original = registry.CreateEntity();

        Assert.True(registry.DestroyEntity(original));
        Assert.False(registry.DestroyEntity(original));
        Assert.False(registry.DestroyEntity(EntityId.Invalid));
        Assert.Equal(EntityId.FromParts(1, 2), registry.CreateEntity());
        Assert.Equal(EntityId.FromParts(2, 1), registry.CreateEntity());
        Assert.Equal(2, registry.AliveCount);
    }

    [Fact]
    public void ReusableSlotsFollowTheExplicitLastDestroyedFirstPolicy()
    {
        var registry = new EntityRegistry();
        EntityId first = registry.CreateEntity();
        EntityId second = registry.CreateEntity();
        EntityId third = registry.CreateEntity();
        registry.DestroyEntity(first);
        registry.DestroyEntity(third);

        Assert.Equal(EntityId.FromParts(third.Index, 2), registry.CreateEntity());
        Assert.Equal(EntityId.FromParts(first.Index, 2), registry.CreateEntity());
        Assert.True(registry.IsAlive(second));
    }

    [Fact]
    public void EnumerationUsesAscendingSlotOrderEvenWhenGenerationsDiffer()
    {
        var registry = new EntityRegistry();
        EntityId first = registry.CreateEntity();
        EntityId second = registry.CreateEntity();
        EntityId third = registry.CreateEntity();
        registry.DestroyEntity(first);
        registry.DestroyEntity(second);
        EntityId reusedSecond = registry.CreateEntity();

        var observed = new List<EntityId>();
        foreach (EntityId entity in registry.GetAliveEntities())
        {
            observed.Add(entity);
        }

        Assert.Equal(new[] { reusedSecond, third }, observed);
        Assert.True(reusedSecond.Value > third.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StructuralMutationInvalidatesActiveEnumeration(bool create)
    {
        var registry = new EntityRegistry();
        EntityId entity = registry.CreateEntity();
        EntityRegistry.AliveEntityEnumerator enumerator = registry.GetAliveEntities().GetEnumerator();
        Assert.True(enumerator.MoveNext());

        if (create)
        {
            registry.CreateEntity();
        }
        else
        {
            registry.DestroyEntity(entity);
        }

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void EnumeratorCurrentRequiresAValidPosition()
    {
        var registry = new EntityRegistry();
        registry.CreateEntity();
        EntityRegistry.AliveEntityEnumerator enumerator = registry.GetAliveEntities().GetEnumerator();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void EqualAllocatorHistoryHashesEquallyRegardlessOfCapacity()
    {
        var first = new EntityRegistry(1);
        var second = new EntityRegistry(64);
        for (int index = 0; index < 37; index++)
        {
            Assert.Equal(first.CreateEntity(), second.CreateEntity());
        }

        first.DestroyEntity(EntityId.FromParts(7, 1));
        second.DestroyEntity(EntityId.FromParts(7, 1));
        Assert.Equal(Hash(first), Hash(second));
    }

    [Fact]
    public void GenerationChangesArePartOfAllocatorStateHash()
    {
        var original = new EntityRegistry();
        var reused = new EntityRegistry();
        original.CreateEntity();
        EntityId stale = reused.CreateEntity();
        reused.DestroyEntity(stale);
        reused.CreateEntity();

        Assert.Equal(original.AliveCount, reused.AliveCount);
        Assert.Equal(original.AllocatedSlotCount, reused.AllocatedSlotCount);
        Assert.NotEqual(Hash(original), Hash(reused));
    }

    [Fact]
    public void FreeListOrderAffectsHashAndFutureAllocations()
    {
        var first = new EntityRegistry();
        var second = new EntityRegistry();
        EntityId a = first.CreateEntity();
        EntityId b = first.CreateEntity();
        second.CreateEntity();
        second.CreateEntity();
        first.DestroyEntity(a);
        first.DestroyEntity(b);
        second.DestroyEntity(b);
        second.DestroyEntity(a);

        Assert.Equal(0, first.AliveCount);
        Assert.Equal(0, second.AliveCount);
        Assert.NotEqual(Hash(first), Hash(second));
        Assert.NotEqual(first.CreateEntity(), second.CreateEntity());
    }

    private static ulong Hash(EntityRegistry registry)
    {
        var hasher = new StableHasher64();
        registry.ContributeToHash(hasher);
        return hasher.Value;
    }
}
