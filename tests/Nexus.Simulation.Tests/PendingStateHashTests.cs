using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class PendingStateHashTests
{
    [Fact]
    public void SamePendingCommandsInSameOrderProduceSameHash()
    {
        CommandBuffer first = CreateCommands(new ValueCommand(10), new OtherCommand(20));
        CommandBuffer second = CreateCommands(new ValueCommand(10), new OtherCommand(20));

        Assert.Equal(Compute(first, new EventBuffer()), Compute(second, new EventBuffer()));
    }

    [Fact]
    public void DifferentPendingCommandPayloadProducesDifferentHash()
    {
        CommandBuffer first = CreateCommands(new ValueCommand(10));
        CommandBuffer second = CreateCommands(new ValueCommand(20));

        Assert.NotEqual(Compute(first, new EventBuffer()), Compute(second, new EventBuffer()));
    }

    [Fact]
    public void DifferentPendingCommandTypeProducesDifferentHash()
    {
        CommandBuffer first = CreateCommands(new ValueCommand(10));
        CommandBuffer second = CreateCommands(new OtherCommand(10));

        Assert.NotEqual(Compute(first, new EventBuffer()), Compute(second, new EventBuffer()));
    }

    [Fact]
    public void DifferentPendingCommandOrderProducesDifferentHash()
    {
        CommandBuffer first = CreateCommands(new ValueCommand(10), new OtherCommand(20));
        CommandBuffer second = CreateCommands(new OtherCommand(20), new ValueCommand(10));

        Assert.NotEqual(Compute(first, new EventBuffer()), Compute(second, new EventBuffer()));
    }

    [Fact]
    public void DifferentPublishedEventPayloadProducesDifferentHash()
    {
        EventBuffer first = CreateEvents(new ValueEvent(10));
        EventBuffer second = CreateEvents(new ValueEvent(20));

        Assert.NotEqual(Compute(new CommandBuffer(), first), Compute(new CommandBuffer(), second));
    }

    [Fact]
    public void DifferentPublishedEventTypeProducesDifferentHash()
    {
        EventBuffer first = CreateEvents(new ValueEvent(10));
        EventBuffer second = CreateEvents(new OtherEvent(10));

        Assert.NotEqual(Compute(new CommandBuffer(), first), Compute(new CommandBuffer(), second));
    }

    [Fact]
    public void DifferentPublishedEventOrderProducesDifferentHash()
    {
        EventBuffer first = CreateEvents(new ValueEvent(10), new OtherEvent(20));
        EventBuffer second = CreateEvents(new OtherEvent(20), new ValueEvent(10));

        Assert.NotEqual(Compute(new CommandBuffer(), first), Compute(new CommandBuffer(), second));
    }

    [Fact]
    public void RebuiltPipelinesProduceTheSameCompleteStateHash()
    {
        SimulationPipeline first = CreateFutureStatePipeline();
        SimulationPipeline second = CreateFutureStatePipeline();

        ulong firstHash = first.Run().StateHash;
        ulong secondHash = second.Run().StateHash;

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void SystemFrequencyIsPartOfTheStateHashProtocol()
    {
        var everyTick = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(123), 60));
        var everyTwoTicks = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(123), 60));
        everyTick.RegisterSystem(new ScheduledSystem(SimulationSystemFrequency.EveryTick));
        everyTwoTicks.RegisterSystem(new ScheduledSystem(SimulationSystemFrequency.EveryTwoTicks));

        Assert.NotEqual(everyTick.ComputeStateHash(), everyTwoTicks.ComputeStateHash());
    }

    [Fact]
    public void OpenEventBufferCannotBeHashedAsACompleteState()
    {
        var events = new EventBuffer();
        events.BeginTick();
        events.Publish(new ValueEvent(10));

        Assert.Throws<InvalidOperationException>(() => Compute(new CommandBuffer(), events));
    }

    [Fact]
    public void PendingCommandExecutionScopeIsPartOfTheStateHash()
    {
        SimulationPipeline firstOrigin = CreateScopedCommandPipeline("first");
        SimulationPipeline secondOrigin = CreateScopedCommandPipeline("second");

        Assert.NotEqual(firstOrigin.Run().StateHash, secondOrigin.Run().StateHash);
    }

    [Fact]
    public void PipelineRejectsStateHashDuringAnOpenTick()
    {
        SimulationPipeline? pipeline = null;
        pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(123), 60, 1));
        pipeline.RegisterSystem(new ReentrantHashSystem(() => pipeline));

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(pipeline.Run);

        Assert.Contains("completed tick boundary", exception.Message, StringComparison.Ordinal);
        Assert.True(pipeline.IsFaulted);
    }

    [Fact]
    public void CommandBufferRejectsHashDuringProcessingAndClearsItsPhaseGuard()
    {
        var commands = new CommandBuffer();
        var events = new EventBuffer();
        var seed = new DeterministicSeed(123);
        var context = new SimulationContext(
            new SimulationClock(60),
            new RandomStreamProvider(seed),
            commands,
            events);
        commands.Enqueue(new HashDuringExecutionCommand(commands, events));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => commands.ProcessPending(context));

        Assert.Contains("completed phase boundary", exception.Message, StringComparison.Ordinal);
        _ = Compute(commands, events);
        commands.Enqueue(new ValueCommand(10));
        Assert.Equal(1, commands.ProcessPending(context));
    }

    private static CommandBuffer CreateCommands(params ISimulationCommand[] values)
    {
        var commands = new CommandBuffer();
        foreach (ISimulationCommand value in values)
        {
            commands.Enqueue(value);
        }

        return commands;
    }

    private static EventBuffer CreateEvents(params ISimulationEvent[] values)
    {
        var events = new EventBuffer();
        events.BeginTick();
        foreach (ISimulationEvent value in values)
        {
            events.Publish(value);
        }

        events.CompleteTick();
        return events;
    }

    private static SimulationStateHash Compute(CommandBuffer commands, EventBuffer events)
    {
        var seed = new DeterministicSeed(123);
        return SimulationStateHasher.Compute(
            seed,
            60,
            new SimulationTick(1),
            new RandomStreamProvider(seed),
            commands,
            events,
            [],
            []);
    }

    private static SimulationPipeline CreateFutureStatePipeline()
    {
        var pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(123), 60, 1));
        pipeline.RegisterSystem(new FutureStateSystem());
        return pipeline;
    }

    private static SimulationPipeline CreateScopedCommandPipeline(string selectedSystemId)
    {
        var pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(123), 60, 1));
        var first = new OriginSystem("first", 10, selectedSystemId == "first");
        var second = new OriginSystem("second", 20, selectedSystemId == "second");
        pipeline.RegisterSystem(first);
        pipeline.RegisterSystem(second);
        pipeline.RegisterStateContributor(first);
        pipeline.RegisterStateContributor(second);
        return pipeline;
    }

    private sealed class ValueCommand(int value) : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.value-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(value);

        public void Execute(ISimulationContext context)
        {
        }
    }

    private sealed class OtherCommand(int value) : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.other-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(value);

        public void Execute(ISimulationContext context)
        {
        }
    }

    private sealed class ParentCommand(int childValue) : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.parent-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(childValue);

        public void Execute(ISimulationContext context) =>
            context.EnqueueCommand(new ValueCommand(childValue));
    }

    private sealed class ScopeParentCommand : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.scope-parent-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
        }

        public void Execute(ISimulationContext context) =>
            context.EnqueueCommand(new ScopedRandomCommand());
    }

    private sealed class ScopedRandomCommand : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.scoped-random-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
        }

        public void Execute(ISimulationContext context) =>
            _ = context.GetRandomStream("command").NextUInt32();
    }

    private sealed class HashDuringExecutionCommand(
        CommandBuffer commands,
        EventBuffer events) : ISimulationCommand
    {
        public string StableTypeId => "tests.pending.hash-during-execution-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add("owning-buffers");

        public void Execute(ISimulationContext context) => _ = Compute(commands, events);
    }

    private sealed record ValueEvent(int Value) : ISimulationEvent
    {
        public string StableTypeId => "tests.pending.value-event.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(Value);
    }

    private sealed record OtherEvent(int Value) : ISimulationEvent
    {
        public string StableTypeId => "tests.pending.other-event.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(Value);
    }

    private sealed class FutureStateSystem : ISimulationSystem
    {
        public string Id => "tests.pending.future-state";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            context.PublishEvent(new ValueEvent(30));
            context.EnqueueCommand(new ParentCommand(40));
        }
    }

    private sealed class ScheduledSystem(SimulationSystemFrequency frequency) : ISimulationSystem
    {
        public string Id => "tests.pending.scheduled";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => frequency;

        public void Execute(ISimulationContext context)
        {
        }
    }

    private sealed class OriginSystem(
        string id,
        int order,
        bool enqueueCommand) : ISimulationSystem, IStateHashContributor
    {
        private bool _enqueueCommand = enqueueCommand;

        public string Id => id;

        public int Order => order;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            if (!_enqueueCommand)
            {
                return;
            }

            _enqueueCommand = false;
            context.EnqueueCommand(new ScopeParentCommand());
        }

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(_enqueueCommand);
    }

    private sealed class ReentrantHashSystem(
        Func<SimulationPipeline?> getPipeline) : ISimulationSystem
    {
        public string Id => "tests.pending.reentrant-hash";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) =>
            getPipeline()!.ComputeStateHash();
    }
}
