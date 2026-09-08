using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class EcsWorldTests
{
    [Fact]
    public void ComponentCanBeAddedReadCopiedUpdatedAndRemoved()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(7));

        Assert.True(world.HasComponent<TestPosition>(entity));
        Assert.Equal(1, world.ComponentCount<TestPosition>());
        Assert.True(world.TryGetComponent(entity, out TestPosition initial));
        Assert.Equal(7, initial.Value);
        initial.Value = 900;
        Assert.Equal(7, world.GetComponent<TestPosition>(entity).Value);

        world.SetComponent(entity, new TestPosition(42));
        Assert.Equal(42, world.GetComponent<TestPosition>(entity).Value);
        Assert.True(world.RemoveComponent<TestPosition>(entity));
        Assert.False(world.HasComponent<TestPosition>(entity));
        Assert.False(world.TryGetComponent(entity, out TestPosition _));
        Assert.Equal(0, world.ComponentCount<TestPosition>());
        Assert.False(world.RemoveComponent<TestPosition>(entity));
    }

    [Fact]
    public void DuplicateAddDoesNotReplaceTheExistingPayload()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(7));

        Assert.Throws<InvalidOperationException>(() => world.AddComponent(entity, new TestPosition(9)));

        Assert.Equal(7, world.GetComponent<TestPosition>(entity).Value);
        Assert.Equal(1, world.ComponentCount<TestPosition>());
    }

    [Fact]
    public void DestroyRemovesEveryComponentAndStaleIdCannotReachReplacement()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId stale = world.CreateEntity();
        world.AddComponent(stale, new TestPosition(3));
        world.AddComponent(stale, new TestVelocity(4));

        Assert.True(world.DestroyEntity(stale));
        Assert.False(world.DestroyEntity(stale));
        EntityId replacement = world.CreateEntity();
        world.AddComponent(replacement, new TestPosition(99));

        Assert.NotEqual(stale, replacement);
        Assert.False(world.IsAlive(stale));
        Assert.True(world.IsAlive(replacement));
        Assert.False(world.HasComponent<TestPosition>(stale));
        Assert.False(world.TryGetComponent(stale, out TestPosition _));
        Assert.False(world.RemoveComponent<TestPosition>(stale));
        Assert.Throws<InvalidOperationException>(() => world.AddComponent(stale, new TestPosition(5)));
        Assert.Throws<InvalidOperationException>(() => world.SetComponent(stale, new TestPosition(5)));
        Assert.Equal(99, world.GetComponent<TestPosition>(replacement).Value);
        Assert.Equal(1, world.ComponentCount<TestPosition>());
        Assert.Equal(0, world.ComponentCount<TestVelocity>());
        Assert.Equal(1, world.EntityCount);
    }

    [Fact]
    public void CopyEntitiesContainsOnlyLiveGenerationsAndChecksDestinationCapacity()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId first = world.CreateEntity();
        EntityId destroyed = world.CreateEntity();
        EntityId third = world.CreateEntity();
        world.DestroyEntity(destroyed);
        EntityId replacement = world.CreateEntity();
        var ids = new EntityId[3];

        Assert.Equal(3, world.CopyEntities(ids));
        Assert.Contains(first, ids);
        Assert.Contains(third, ids);
        Assert.Contains(replacement, ids);
        Assert.DoesNotContain(destroyed, ids);
        Assert.Throws<ArgumentException>(() => world.CopyEntities(new EntityId[2]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ComponentSchemaFreezesAtAnExplicitOrObservableBoundary(int boundary)
    {
        var world = new EcsWorld("freeze", 10);
        world.RegisterComponent<TestPosition>();

        switch (boundary)
        {
            case 0:
                world.FreezeComponents();
                break;
            case 1:
                world.CreateEntity();
                break;
            case 2:
                var action = new NoOpAction();
                world.Query<TestPosition, NoOpAction>(ref action);
                break;
            default:
                EcsTestWorld.Hash(world);
                break;
        }

        Assert.Throws<InvalidOperationException>(world.RegisterComponent<TestVelocity>);
    }

    [Fact]
    public void DuplicateRuntimeTypesAndDuplicateSemanticIdsAreRejected()
    {
        var world = new EcsWorld("registration", 10);
        world.RegisterComponent<TestPosition>();

        Assert.Throws<InvalidOperationException>(world.RegisterComponent<TestPosition>);
        Assert.Throws<InvalidOperationException>(world.RegisterComponent<ConflictingPosition>);
    }

    private struct NoOpAction : IQueryAction<TestPosition>
    {
        public readonly void Execute(EntityId entity, ref TestPosition first)
        {
        }
    }

    private readonly struct ConflictingPosition : IComponent<ConflictingPosition>
    {
        public static string StableId => TestPosition.StableId;

        public static void ContributeToHash(in ConflictingPosition value, StableHasher64 hasher) => hasher.Add(0);
    }
}
