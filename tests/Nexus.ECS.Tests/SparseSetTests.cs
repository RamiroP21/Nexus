using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class SparseSetTests
{
    [Fact]
    public void AddStoresContiguousEntityAndComponentPairs()
    {
        var store = new SparseSet<TestValue>();
        EntityId first = EntityId.FromParts(4, 1);
        EntityId second = EntityId.FromParts(1, 1);
        store.Add(first, new TestValue(41));
        store.Add(second, new TestValue(12));

        Assert.Equal(2, store.Count);
        Assert.Equal(first, store.GetEntityAt(0));
        Assert.Equal(second, store.GetEntityAt(1));
        Assert.Equal(41, store.GetAt(0).Value);
        Assert.Equal(12, store.Get(second).Value);
        Assert.True(store.Has(first));
        Assert.True(store.TryGet(second, out TestValue copied));
        Assert.Equal(12, copied.Value);
    }

    [Fact]
    public void RefAccessMutatesTheStoredValueWithoutCopying()
    {
        var store = new SparseSet<TestValue>();
        EntityId entity = EntityId.FromParts(1, 1);
        store.Add(entity, new TestValue(1));

        ref TestValue byEntity = ref store.Get(entity);
        byEntity.Value = 13;
        ref TestValue byDenseIndex = ref store.GetAt(0);
        byDenseIndex.Value += 7;

        Assert.Equal(20, store.Get(entity).Value);
    }

    [Fact]
    public void MissingComponentsHaveExplicitLookupAndRemovalBehavior()
    {
        var store = new SparseSet<TestValue>();
        EntityId entity = EntityId.FromParts(1, 1);

        Assert.False(store.Has(entity));
        Assert.False(store.Has(EntityId.Invalid));
        Assert.False(store.Has(new EntityId(ulong.MaxValue)));
        Assert.False(store.TryGet(entity, out TestValue value));
        Assert.Equal(default, value);
        Assert.False(store.Remove(entity));
        Assert.Throws<KeyNotFoundException>(() => store.Get(entity));
        Assert.Equal(0, store.Count);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(4294967296UL)]
    public void AddRejectsIdentitiesWithoutRegistryIndexAndGeneration(ulong rawValue)
    {
        var store = new SparseSet<TestValue>();

        Assert.Throws<ArgumentException>(() => store.Add(new EntityId(rawValue), new TestValue(1)));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void DuplicateAddDoesNotReplaceTheExistingComponent()
    {
        var store = new SparseSet<TestValue>();
        EntityId entity = EntityId.FromParts(1, 1);
        store.Add(entity, new TestValue(10));

        Assert.Throws<InvalidOperationException>(() => store.Add(entity, new TestValue(20)));
        Assert.Equal(1, store.Count);
        Assert.Equal(10, store.Get(entity).Value);
    }

    [Fact]
    public void AnotherGenerationCannotAliasOrReplaceAnOccupiedSlot()
    {
        var store = new SparseSet<TestValue>();
        EntityId original = EntityId.FromParts(1, 1);
        EntityId replacement = EntityId.FromParts(1, 2);
        store.Add(original, new TestValue(10));

        Assert.False(store.Has(replacement));
        Assert.False(store.TryGet(replacement, out _));
        Assert.False(store.Remove(replacement));
        Assert.Throws<KeyNotFoundException>(() => store.Get(replacement));
        Assert.Throws<InvalidOperationException>(() => store.Add(replacement, new TestValue(20)));
        Assert.Equal(10, store.Get(original).Value);
    }

    [Fact]
    public void SwapBackPreservesDenseSparseAndPayloadConsistency()
    {
        var store = new SparseSet<TestValue>();
        EntityId first = EntityId.FromParts(1, 1);
        EntityId middle = EntityId.FromParts(2, 1);
        EntityId last = EntityId.FromParts(3, 1);
        store.Add(first, new TestValue(11));
        store.Add(middle, new TestValue(22));
        store.Add(last, new TestValue(33));

        Assert.True(store.Remove(middle));

        Assert.Equal(2, store.Count);
        Assert.Equal(first, store.GetEntityAt(0));
        Assert.Equal(last, store.GetEntityAt(1));
        Assert.Equal(33, store.GetAt(1).Value);
        Assert.Equal(33, store.Get(last).Value);
        Assert.False(store.Has(middle));
        Assert.True(store.Remove(last));
        Assert.True(store.Remove(first));
        Assert.Equal(0, store.Count);
        Assert.False(store.Has(first));
        Assert.False(store.Has(last));
    }

    [Fact]
    public void SparseAndDenseArraysGrowIndependently()
    {
        var store = new SparseSet<TestValue>(1);
        for (uint index = 1; index <= 513; index++)
        {
            store.Add(EntityId.FromParts(index * 5, 1), new TestValue((int)index));
        }

        Assert.Equal(513, store.Count);
        Assert.True(store.Capacity >= 513);
        for (uint index = 1; index <= 513; index++)
        {
            Assert.Equal((int)index, store.Get(EntityId.FromParts(index * 5, 1)).Value);
        }
    }

    [Fact]
    public void RepeatedSwapBackMaintainsLookupForEveryDenseEntry()
    {
        var store = new SparseSet<TestValue>(1);
        for (uint index = 1; index <= 128; index++)
        {
            store.Add(EntityId.FromParts(index, 1), new TestValue((int)index * 10));
        }

        for (uint index = 2; index <= 128; index += 2)
        {
            Assert.True(store.Remove(EntityId.FromParts(index, 1)));
        }

        Assert.Equal(64, store.Count);
        for (int index = 0; index < store.Count; index++)
        {
            EntityId entity = store.GetEntityAt(index);
            Assert.True(store.Has(entity));
            Assert.Equal(1U, entity.Index % 2);
            Assert.Equal((int)entity.Index * 10, store.GetAt(index).Value);
            Assert.Equal(store.GetAt(index), store.Get(entity));
        }
    }

    [Fact]
    public void RemovedSlotCanBeReusedWithoutRevivingTheOldGeneration()
    {
        var store = new SparseSet<TestValue>();
        EntityId oldEntity = EntityId.FromParts(1, 1);
        EntityId newEntity = EntityId.FromParts(1, 2);
        store.Add(oldEntity, new TestValue(8));
        store.Remove(oldEntity);
        store.Add(newEntity, new TestValue(9));

        Assert.False(store.Has(oldEntity));
        Assert.True(store.Has(newEntity));
        Assert.Equal(newEntity, store.GetEntityAt(0));
        Assert.Equal(9, store.Get(newEntity).Value);
    }

    [Fact]
    public void DenseAccessRejectsNegativeAndUnusedPositions()
    {
        var store = new SparseSet<TestValue>();
        store.Add(EntityId.FromParts(1, 1), new TestValue(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => store.GetAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.GetAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.GetEntityAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.GetEntityAt(1));
    }

    [Fact]
    public void IdenticalHistoryHashesEquallyAcrossStorageCapacities()
    {
        var small = new SparseSet<TestValue>(1);
        var large = new SparseSet<TestValue>(128);
        for (uint index = 1; index <= 17; index++)
        {
            var value = new TestValue((int)index);
            EntityId entity = EntityId.FromParts(index, 1);
            small.Add(entity, value);
            large.Add(entity, value);
        }

        small.Remove(EntityId.FromParts(7, 1));
        large.Remove(EntityId.FromParts(7, 1));
        Assert.Equal(Hash(small), Hash(large));
    }

    [Fact]
    public void PayloadAndGenerationChangesAffectHash()
    {
        var first = new SparseSet<TestValue>();
        var differentPayload = new SparseSet<TestValue>();
        var differentGeneration = new SparseSet<TestValue>();
        first.Add(EntityId.FromParts(1, 1), new TestValue(1));
        differentPayload.Add(EntityId.FromParts(1, 1), new TestValue(2));
        differentGeneration.Add(EntityId.FromParts(1, 2), new TestValue(1));

        Assert.NotEqual(Hash(first), Hash(differentPayload));
        Assert.NotEqual(Hash(first), Hash(differentGeneration));
    }

    [Fact]
    public void HashIncludesPhysicalOrderBecauseQueriesCanObserveIt()
    {
        var first = new SparseSet<TestValue>();
        var second = new SparseSet<TestValue>();
        EntityId a = EntityId.FromParts(1, 1);
        EntityId b = EntityId.FromParts(2, 1);
        first.Add(a, new TestValue(10));
        first.Add(b, new TestValue(20));
        second.Add(b, new TestValue(20));
        second.Add(a, new TestValue(10));

        Assert.Equal(first.Get(a), second.Get(a));
        Assert.Equal(first.Get(b), second.Get(b));
        Assert.NotEqual(Hash(first), Hash(second));
    }

    private static ulong Hash(SparseSet<TestValue> store)
    {
        var hasher = new StableHasher64();
        store.ContributeToHash(hasher);
        return hasher.Value;
    }

    private record struct TestValue(int Value) : IComponent<TestValue>
    {
        public static string StableId => "nexus.tests.sparse-value.v1";

        public static void ContributeToHash(in TestValue value, StableHasher64 hasher)
        {
            hasher.Add(value.Value);
        }
    }
}
