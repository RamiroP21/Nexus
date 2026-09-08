using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.ECS.Tests;

public sealed class PlaybackBoundaryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void EventCallbackCannotMutateOrHashPartialPlayback(int operation)
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId entity = world.CreateEntity();
        world.AddComponent(entity, new FidelityComponent(SimulationFidelity.Statistical));
        world.Commands.TransitionFidelity(entity, SimulationFidelity.Simplified);
        var context = new CallbackContext(() =>
        {
            switch (operation)
            {
                case 0: world.CreateEntity(); break;
                case 1: world.DestroyEntity(entity); break;
                case 2: world.SetComponent(entity, new FidelityComponent(SimulationFidelity.Full)); break;
                case 3: world.Commands.CreateEntity(); break;
                case 4: world.Commands.Playback(); break;
                default: EcsTestWorld.Hash(world); break;
            }
        });

        Assert.Throws<InvalidOperationException>(() => world.Commands.Playback(context));
        Assert.True(world.IsFaulted);
        Assert.False(world.Commands.IsApplying);
        Assert.Throws<InvalidOperationException>(() => EcsTestWorld.Hash(world));
    }

    [Fact]
    public void StalePendingAddCannotAffectAReusedSlot()
    {
        EcsWorld world = EcsTestWorld.Create();
        EntityId stale = world.CreateEntity();
        world.Commands.AddComponent(stale, new TestPosition(123));
        world.DestroyEntity(stale);
        EntityId replacement = world.CreateEntity();
        Assert.False(world.HasComponent<TestPosition>(replacement));
        Assert.Throws<InvalidOperationException>(() => world.Commands.Playback());
        Assert.True(world.IsFaulted);
    }

    private sealed class CallbackContext(Action callback) : ISimulationContext
    {
        public SimulationTick CurrentTick => default;
        public int TickRate => 60;
        public double LogicalTimeSeconds => 0;
        public double FixedDeltaTimeSeconds => 1.0 / 60;
        public IReadOnlyList<ISimulationEvent> Events => Array.Empty<ISimulationEvent>();
        public ISimulationRandomSource GetRandomStream(string key) => throw new NotSupportedException();
        public void EnqueueCommand(ISimulationCommand command) => throw new NotSupportedException();
        public void PublishEvent(ISimulationEvent simulationEvent) => callback();
    }
}
