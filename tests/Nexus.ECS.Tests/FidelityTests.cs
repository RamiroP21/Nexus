using Nexus.Contracts;
using Nexus.Core;
using Nexus.Simulation;

namespace Nexus.ECS.Tests;

public sealed class FidelityTests
{
    [Theory]
    [InlineData(SimulationFidelity.Statistical, 0)]
    [InlineData(SimulationFidelity.Simplified, 1)]
    [InlineData(SimulationFidelity.Agent, 2)]
    [InlineData(SimulationFidelity.Full, 3)]
    public void EveryFidelityLevelHasExplicitOrdinalAndCanBeStored(SimulationFidelity tier, int ordinal)
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new FidelityComponent(tier));

        Assert.Equal(ordinal, (int)tier);
        Assert.Equal(tier, world.GetComponent<FidelityComponent>(entity).Tier);
    }

    [Fact]
    public void DefaultFidelityIsStatisticalAndUndefinedValuesAreRejected()
    {
        Assert.Equal(SimulationFidelity.Statistical, default(FidelityComponent).Tier);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FidelityComponent((SimulationFidelity)byte.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FidelityComponent((SimulationFidelity)4));
    }

    [Fact]
    public void SequentialPromotionsAndDemotionsValidateAgainstStateAtPlayback()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new FidelityComponent(SimulationFidelity.Statistical));
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Simplified);
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Agent);
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Full);

        Assert.Equal(SimulationFidelity.Statistical, world.GetComponent<FidelityComponent>(entity).Tier);
        Assert.Equal(3, world.Commands.Playback());
        Assert.Equal(SimulationFidelity.Full, world.GetComponent<FidelityComponent>(entity).Tier);

        world.Commands.TransitionFidelity(entity, SimulationFidelity.Agent);
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Simplified);
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Statistical);
        Assert.Equal(3, world.Commands.Playback());
        Assert.Equal(SimulationFidelity.Statistical, world.GetComponent<FidelityComponent>(entity).Tier);
    }

    [Theory]
    [InlineData(SimulationFidelity.Statistical, SimulationFidelity.Statistical)]
    [InlineData(SimulationFidelity.Statistical, SimulationFidelity.Agent)]
    [InlineData(SimulationFidelity.Statistical, SimulationFidelity.Full)]
    [InlineData(SimulationFidelity.Full, SimulationFidelity.Simplified)]
    public void NonAdjacentAndNoOpTransitionsFaultPlayback(SimulationFidelity from, SimulationFidelity to)
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new FidelityComponent(from));
        world.Commands.TransitionFidelity(entity, to);

        Assert.Throws<InvalidOperationException>(() => world.Commands.Playback());
        Assert.True(world.IsFaulted);
        Assert.Throws<InvalidOperationException>(() => EcsTestWorld.Hash(world));
    }

    [Fact]
    public void CurrentFidelityAndPendingTargetBothContributeToWorldHash()
    {
        EcsWorld first = EcsTestWorld.Create();
        EcsWorld second = EcsTestWorld.Create();
        EntityId firstEntity = first.CreateEntity();
        EntityId secondEntity = second.CreateEntity();
        first.AddComponent(firstEntity, new FidelityComponent(SimulationFidelity.Simplified));
        second.AddComponent(secondEntity, new FidelityComponent(SimulationFidelity.Agent));
        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));

        second.SetComponent(secondEntity, new FidelityComponent(SimulationFidelity.Simplified));
        Assert.Equal(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
        first.Commands.TransitionFidelity(firstEntity, SimulationFidelity.Statistical);
        second.Commands.TransitionFidelity(secondEntity, SimulationFidelity.Agent);
        Assert.NotEqual(EcsTestWorld.Hash(first), EcsTestWorld.Hash(second));
    }

    [Fact]
    public void TransitionEventsArePublishedInFifoOrderAndVisibleDuringNextTickOnly()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new FidelityComponent(SimulationFidelity.Statistical));
        var counts = new List<int>();
        var events = new List<FidelityChangedEvent>();
        var pipeline = new SimulationPipeline(new SimulationOptions(new DeterministicSeed(7), 60, 3));
        pipeline.RegisterStateContributor(world);
        pipeline.RegisterSystem(new TransitionProducer(world, entity));
        pipeline.RegisterSystem(new TransitionObserver(counts, events));

        pipeline.Run();

        Assert.Equal([0, 2, 0], counts);
        Assert.Collection(
            events,
            first =>
            {
                Assert.Equal(world.Id, first.WorldId);
                Assert.Equal(entity, first.Entity);
                Assert.Equal(SimulationFidelity.Statistical, first.From);
                Assert.Equal(SimulationFidelity.Simplified, first.To);
            },
            second =>
            {
                Assert.Equal(world.Id, second.WorldId);
                Assert.Equal(entity, second.Entity);
                Assert.Equal(SimulationFidelity.Simplified, second.From);
                Assert.Equal(SimulationFidelity.Agent, second.To);
            });
        Assert.Equal(SimulationFidelity.Agent, world.GetComponent<FidelityComponent>(entity).Tier);
        Assert.Empty(pipeline.PublishedEvents);
    }

    private sealed class TransitionProducer(EcsWorld world, EntityId entity) : ISimulationSystem
    {
        public string Id => "tests.ecs.fidelity-producer.v1";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            if (context.CurrentTick == SimulationTick.Initial)
            {
                world.Commands.TransitionFidelity(entity, SimulationFidelity.Simplified);
                world.Commands.TransitionFidelity(entity, SimulationFidelity.Agent);
                context.EnqueueCommand(world.Commands.AsSimulationCommand());
            }
        }
    }

    private sealed class TransitionObserver(List<int> counts, List<FidelityChangedEvent> events) : ISimulationSystem
    {
        public string Id => "tests.ecs.fidelity-observer.v1";

        public int Order => 20;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            counts.Add(context.Events.Count);
            foreach (ISimulationEvent simulationEvent in context.Events)
            {
                events.Add((FidelityChangedEvent)simulationEvent);
            }
        }
    }
}
