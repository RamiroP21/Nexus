using Nexus.Contracts;
using Nexus.Core;
using Nexus.Simulation;

namespace Nexus.ECS.Tests;

public sealed class StructuralCommandTests
{
    [Fact]
    public void DeferredCreateBecomesVisibleOnlyAtPlayback()
    {
        EcsWorld world = EcsTestWorld.Create();
        world.Commands.CreateEntity();
        world.Commands.CreateEntity();

        Assert.Equal(0, world.EntityCount);
        Assert.Equal(2, world.Commands.Count);
        Assert.Equal(2, world.Commands.Playback());
        Assert.Equal(2, world.EntityCount);
        Assert.Equal(0, world.Commands.Count);
        Assert.Equal(0, world.Commands.Playback());
    }

    [Fact]
    public void DestroyRequestedDuringQueryPreservesIterationAndAppliesAtPlayback()
    {
        EcsWorld world = EcsTestWorld.Create();
        for (int i = 0; i < 9; i++)
        {
            world.AddComponent(world.CreateEntity(), new TestPosition(i));
        }

        var action = new DestroyDuringQueryAction(world);
        world.Query<TestPosition, DestroyDuringQueryAction>(ref action);

        Assert.Equal(9, action.Visited);
        Assert.Equal(9, world.EntityCount);
        Assert.Equal(5, world.Commands.Count);
        Assert.Equal(5, world.Commands.Playback());
        Assert.Equal(4, world.EntityCount);
        Assert.Equal(4, world.ComponentCount<TestPosition>());
    }

    [Fact]
    public void AddAndRemoveRequestedDuringQueryStayDeferredUntilPlayback()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(12));
        var action = new ReplaceDuringQueryAction(world);

        world.Query<TestPosition, ReplaceDuringQueryAction>(ref action);

        Assert.True(world.HasComponent<TestPosition>(entity));
        Assert.False(world.HasComponent<TestVelocity>(entity));
        Assert.Equal(2, world.Commands.Playback());
        Assert.False(world.HasComponent<TestPosition>(entity));
        Assert.Equal(12, world.GetComponent<TestVelocity>(entity).Value);
    }

    [Fact]
    public void PlaybackPreservesFifoAndCopiesPayloadAtEnqueue()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(1));
        var replacement = new TestPosition(7);
        world.Commands.RemoveComponent<TestPosition>(entity);
        world.Commands.AddComponent(entity, replacement);
        replacement.Value = 900;

        Assert.Equal(2, world.Commands.Playback());

        Assert.Equal(7, world.GetComponent<TestPosition>(entity).Value);
    }

    [Fact]
    public void PlaybackFailurePermanentlyFaultsWorldAndRejectsIncompleteHash()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(1));
        world.Commands.CreateEntity();
        world.Commands.AddComponent(entity, new TestPosition(2));
        world.Commands.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => world.Commands.Playback());

        Assert.True(world.IsFaulted);
        Assert.Throws<InvalidOperationException>(() => EcsTestWorld.Hash(world));
        Assert.Throws<InvalidOperationException>(() => world.Commands.Playback());
        Assert.Throws<InvalidOperationException>(() => world.CreateEntity());
    }

    [Fact]
    public void SimulationAdapterAppliesStructuralChangesAfterAllSystems()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new TestPosition(1));
        var observations = new List<bool>();
        var pipeline = new SimulationPipeline(new SimulationOptions(new DeterministicSeed(42), 60, 2));
        pipeline.RegisterStateContributor(world);
        pipeline.RegisterSystem(new DeleteProducer(world, entity));
        pipeline.RegisterSystem(new EntityObserver(world, entity, observations));

        pipeline.Run();

        Assert.Equal([true, false], observations);
        Assert.False(world.IsAlive(entity));
        Assert.Equal(0, world.Commands.Count);
    }

    private struct DestroyDuringQueryAction(EcsWorld world) : IQueryAction<TestPosition>
    {
        public int Visited;

        public void Execute(EntityId entity, ref TestPosition first)
        {
            Visited++;
            if (first.Value % 2 == 0)
            {
                world.Commands.DestroyEntity(entity);
            }
        }
    }

    private readonly struct ReplaceDuringQueryAction(EcsWorld world) : IQueryAction<TestPosition>
    {
        public void Execute(EntityId entity, ref TestPosition first)
        {
            world.Commands.AddComponent(entity, new TestVelocity(first.Value));
            world.Commands.RemoveComponent<TestPosition>(entity);
        }
    }

    private sealed class DeleteProducer(EcsWorld world, EntityId entity) : ISimulationSystem
    {
        public string Id => "tests.ecs.delete-producer.v1";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            if (context.CurrentTick == SimulationTick.Initial)
            {
                world.Commands.DestroyEntity(entity);
                context.EnqueueCommand(world.Commands.AsSimulationCommand());
            }
        }
    }

    private sealed class EntityObserver(EcsWorld world, EntityId entity, List<bool> observations) : ISimulationSystem
    {
        public string Id => "tests.ecs.entity-observer.v1";

        public int Order => 20;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) => observations.Add(world.IsAlive(entity));
    }
}
