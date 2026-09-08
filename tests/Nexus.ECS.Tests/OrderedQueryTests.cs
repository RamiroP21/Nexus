using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class OrderedQueryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OrderedQueriesUseFullIdentityAndFilterIntersection(int arity)
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId stale = world.CreateEntity();
        EntityId second = world.CreateEntity();
        EntityId third = world.CreateEntity();
        world.DestroyEntity(stale);
        EntityId reused = world.CreateEntity();
        foreach (EntityId entity in new[] { reused, third, second })
        {
            world.AddComponent(entity, new TestPosition(0));
            if (entity != third)
            {
                world.AddComponent(entity, new TestVelocity(2));
                world.AddComponent(entity, new TestValue(3));
            }
        }

        var action = new RecordAction([]);
        var scratch = new EntityId[3];
        switch (arity)
        {
            case 1:
                world.QueryOrdered<TestPosition, RecordAction>(scratch, ref action);
                break;
            case 2:
                world.QueryOrdered<TestPosition, TestVelocity, RecordAction>(scratch, ref action);
                break;
            default:
                world.QueryOrdered<TestPosition, TestVelocity, TestValue, RecordAction>(scratch, ref action);
                break;
        }

        Assert.Equal(arity == 1 ? new[] { second, third, reused } : new[] { second, reused }, action.Entities);
        Assert.Equal(arity == 1 ? 1 : arity == 2 ? 2 : 5, world.GetComponent<TestPosition>(reused).Value);
        Assert.False(world.IsAlive(stale));
    }

    [Fact]
    public void OrderedTraversalIgnoresDenseLayoutWithoutReorderingStorage()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId first = world.CreateEntity();
        EntityId second = world.CreateEntity();
        world.AddComponent(second, new TestPosition(0));
        world.AddComponent(first, new TestPosition(0));
        var sorted = new CollectOnly([]);
        ulong before = EcsTestWorld.Hash(world);
        world.QueryOrdered<TestPosition, CollectOnly>(new EntityId[2], ref sorted);
        var dense = new CollectOnly([]);
        world.Query<TestPosition, CollectOnly>(ref dense);

        Assert.Equal(new[] { first, second }, sorted.Entities);
        Assert.Equal(new[] { second, first }, dense.Entities);
        Assert.Equal(before, EcsTestWorld.Hash(world));
    }

    [Fact]
    public void InsufficientScratchRejectsBeforeExecutingAnyCallback()
    {
        EcsWorld world = EcsTestWorld.Create();
        world.AddComponent(world.CreateEntity(), new TestPosition(0));
        var action = new RecordAction([]);
        Assert.Throws<ArgumentException>(() => world.QueryOrdered<TestPosition, RecordAction>(Array.Empty<EntityId>(), ref action));
        Assert.Empty(action.Entities);
        Assert.True(world.DestroyEntity(EntityId.FromParts(1, 1)));
    }

    [Fact]
    public void OrderedQueryGuardsStructureAndDefersDestruction()
    {
        EcsWorld world = EcsTestWorld.Create();
        world.AddComponent(world.CreateEntity(), new TestPosition(0));
        var action = new DeferDestroy(world);
        world.QueryOrdered<TestPosition, DeferDestroy>(new EntityId[1], ref action);
        Assert.Equal(1, world.EntityCount);
        Assert.Equal(1, world.Commands.Playback());
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void ThrowingCallbackReleasesQueryGuard()
    {
        EcsWorld world = EcsTestWorld.Create();
        world.AddComponent(world.CreateEntity(), new TestPosition(0));
        var action = new ThrowAction();
        Assert.Throws<InvalidOperationException>(() => world.QueryOrdered<TestPosition, ThrowAction>(new EntityId[1], ref action));
        Assert.True(world.DestroyEntity(EntityId.FromParts(1, 1)));
    }

    [Fact]
    public void ReusedOrderedScratchDoesNotAllocateInWarmQueries()
    {
        EcsWorld world = EcsTestWorld.Create();
        for (int i = 0; i < 32; i++)
        {
            world.AddComponent(world.CreateEntity(), new TestPosition(0));
        }

        var scratch = new EntityId[32];
        var action = new Increment();
        for (int i = 0; i < 100; i++)
        {
            world.QueryOrdered<TestPosition, Increment>(scratch, ref action);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            world.QueryOrdered<TestPosition, Increment>(scratch, ref action);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private readonly struct RecordAction(List<EntityId> entities) :
        IQueryAction<TestPosition>, IQueryAction<TestPosition, TestVelocity>, IQueryAction<TestPosition, TestVelocity, TestValue>
    {
        public List<EntityId> Entities => entities;

        public void Execute(EntityId entity, ref TestPosition first)
        {
            entities.Add(entity);
            first.Value++;
        }

        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second)
        {
            entities.Add(entity);
            first.Value += second.Value;
        }

        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second, ref TestValue third)
        {
            entities.Add(entity);
            first.Value += second.Value + third.Value;
        }
    }

    private readonly struct CollectOnly(List<EntityId> entities) : IQueryAction<TestPosition>
    {
        public List<EntityId> Entities => entities;

        public void Execute(EntityId entity, ref TestPosition first) => entities.Add(entity);
    }

    private readonly struct DeferDestroy(EcsWorld world) : IQueryAction<TestPosition>
    {
        public void Execute(EntityId entity, ref TestPosition first)
        {
            EcsWorld target = world;
            Assert.Throws<InvalidOperationException>(() => target.DestroyEntity(entity));
            Assert.Throws<InvalidOperationException>(() => EcsTestWorld.Hash(target));
            world.Commands.DestroyEntity(entity);
        }
    }

    private readonly struct ThrowAction : IQueryAction<TestPosition>
    {
        public void Execute(EntityId entity, ref TestPosition first) => throw new InvalidOperationException("test callback");
    }

    private readonly struct Increment : IQueryAction<TestPosition>
    {
        public void Execute(EntityId entity, ref TestPosition first) => first.Value++;
    }
}
