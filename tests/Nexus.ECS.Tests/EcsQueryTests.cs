using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class EcsQueryTests
{
    [Fact]
    public void SingleComponentQueryMutatesOnlyMatchingLiveEntities()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId first = world.CreateEntity();
        EntityId removed = world.CreateEntity();
        EntityId destroyed = world.CreateEntity();
        EntityId noPosition = world.CreateEntity();
        world.AddComponent(first, new TestPosition(10));
        world.AddComponent(removed, new TestPosition(20));
        world.AddComponent(destroyed, new TestPosition(30));
        world.RemoveComponent<TestPosition>(removed);
        world.DestroyEntity(destroyed);
        var action = new IncrementAction();

        world.Query<TestPosition, IncrementAction>(ref action);

        Assert.Equal(1, action.Count);
        Assert.Equal(11, world.GetComponent<TestPosition>(first).Value);
        Assert.False(world.HasComponent<TestPosition>(removed));
        Assert.False(world.HasComponent<TestPosition>(destroyed));
        Assert.False(world.HasComponent<TestPosition>(noPosition));
    }

    [Fact]
    public void TwoComponentQueryUsesFirstStoreDenseOrderAndReturnsMatchingReferences()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId first = world.CreateEntity();
        EntityId second = world.CreateEntity();
        EntityId third = world.CreateEntity();
        EntityId onlyPosition = world.CreateEntity();
        world.AddComponent(first, new TestPosition(1));
        world.AddComponent(second, new TestPosition(2));
        world.AddComponent(third, new TestPosition(3));
        world.AddComponent(onlyPosition, new TestPosition(4));
        world.AddComponent(third, new TestVelocity(30));
        world.AddComponent(first, new TestVelocity(10));
        world.AddComponent(second, new TestVelocity(20));
        world.RemoveComponent<TestPosition>(second);
        var action = new TracePairAction(new EntityId[3]);

        world.Query<TestPosition, TestVelocity, TracePairAction>(ref action);

        Assert.Equal(2, action.Count);
        Assert.Equal(first, action.Entities[0]);
        Assert.Equal(third, action.Entities[1]);
        Assert.Equal(11, world.GetComponent<TestPosition>(first).Value);
        Assert.Equal(33, world.GetComponent<TestPosition>(third).Value);
        Assert.Equal(11, world.GetComponent<TestVelocity>(first).Value);
        Assert.Equal(20, world.GetComponent<TestVelocity>(second).Value);
    }

    [Fact]
    public void ThreeComponentQueryIntersectsAllStoresAndMutatesEachPayload()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId complete = world.CreateEntity();
        EntityId pair = world.CreateEntity();
        world.AddComponent(complete, new TestPosition(2));
        world.AddComponent(complete, new TestVelocity(3));
        world.AddComponent(complete, new TestValue(4));
        world.AddComponent(pair, new TestPosition(8));
        world.AddComponent(pair, new TestVelocity(9));
        var action = new TripleAction();

        world.Query<TestPosition, TestVelocity, TestValue, TripleAction>(ref action);

        Assert.Equal(1, action.Count);
        Assert.Equal(5, world.GetComponent<TestPosition>(complete).Value);
        Assert.Equal(7, world.GetComponent<TestVelocity>(complete).Value);
        Assert.Equal(5, world.GetComponent<TestValue>(complete).Value);
        Assert.Equal(8, world.GetComponent<TestPosition>(pair).Value);
    }

    [Fact]
    public void SwapBackChangesQueryOrderInAnExplicitReproducibleWay()
    {
        EcsWorld world = EcsTestWorld.Create();
        var ids = new EntityId[4];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = world.CreateEntity();
            world.AddComponent(ids[i], new TestPosition(i));
        }

        world.RemoveComponent<TestPosition>(ids[1]);
        var action = new TraceSingleAction(new EntityId[3]);
        world.Query<TestPosition, TraceSingleAction>(ref action);

        Assert.Equal([ids[0], ids[3], ids[2]], action.Entities);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ActiveQueryRejectsImmediateStructuralChangesPlaybackAndHashing(int operation)
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(5));
        world.Commands.CreateEntity();
        var action = new GuardAction(world, operation);

        world.Query<TestPosition, GuardAction>(ref action);

        Assert.True(action.Rejected);
        Assert.Equal(1, world.EntityCount);
        Assert.Equal(1, world.ComponentCount<TestPosition>());
        Assert.Equal(0, world.ComponentCount<TestVelocity>());
        Assert.Equal(1, world.Commands.Count);
        Assert.False(world.IsFaulted);
        Assert.Equal(1, world.Commands.Playback());
        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void WarmSingleComponentQueriesDoNotAllocatePerEntity()
    {
        EcsWorld world = EcsTestWorld.Create(1_024);
        for (int i = 0; i < 1_000; i++)
        {
            world.AddComponent(world.CreateEntity(), new TestPosition(i));
        }

        var action = new IncrementAction();
        world.Query<TestPosition, IncrementAction>(ref action);
        long before = GC.GetAllocatedBytesForCurrentThread();
        world.Query<TestPosition, IncrementAction>(ref action);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(2_000, action.Count);
        Assert.Equal(0L, allocated);
    }

    private struct IncrementAction : IQueryAction<TestPosition>
    {
        public int Count;

        public void Execute(EntityId entity, ref TestPosition first)
        {
            first.Value++;
            Count++;
        }
    }

    private struct TraceSingleAction(EntityId[] entities) : IQueryAction<TestPosition>
    {
        public readonly EntityId[] Entities = entities;
        private int _count;

        public void Execute(EntityId entity, ref TestPosition first) => Entities[_count++] = entity;
    }

    private struct TracePairAction(EntityId[] entities) : IQueryAction<TestPosition, TestVelocity>
    {
        public readonly EntityId[] Entities = entities;
        public int Count;

        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second)
        {
            Entities[Count++] = entity;
            first.Value += second.Value;
            second.Value++;
        }
    }

    private struct TripleAction : IQueryAction<TestPosition, TestVelocity, TestValue>
    {
        public int Count;

        public void Execute(EntityId entity, ref TestPosition first, ref TestVelocity second, ref TestValue third)
        {
            first.Value += second.Value;
            second.Value += third.Value;
            third.Value++;
            Count++;
        }
    }

    private struct GuardAction(EcsWorld world, int operation) : IQueryAction<TestPosition>
    {
        public bool Rejected;

        public void Execute(EntityId entity, ref TestPosition first)
        {
            try
            {
                switch (operation)
                {
                    case 0:
                        world.CreateEntity();
                        break;
                    case 1:
                        world.DestroyEntity(entity);
                        break;
                    case 2:
                        world.AddComponent(entity, new TestVelocity(4));
                        break;
                    case 3:
                        world.RemoveComponent<TestPosition>(entity);
                        break;
                    case 4:
                        world.Commands.Playback();
                        break;
                    default:
                        EcsTestWorld.Hash(world);
                        break;
                }
            }
            catch (InvalidOperationException)
            {
                Rejected = true;
            }
        }
    }
}
