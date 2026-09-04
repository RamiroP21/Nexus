using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class SimulationPipelineTests
{
    [Fact]
    public void PipelineExecutesSystemsInExplicitOrder()
    {
        var trace = new List<string>();
        var pipeline = CreatePipeline(1);
        pipeline.RegisterSystem(new RecordingSystem("third", 30, trace));
        pipeline.RegisterSystem(new RecordingSystem("first", 10, trace));
        pipeline.RegisterSystem(new RecordingSystem("second", 20, trace));

        pipeline.Run();

        Assert.Equal(["first:0", "second:0", "third:0"], trace);
    }

    [Fact]
    public void FreezeRejectsDuplicateSystemOrders()
    {
        var pipeline = CreatePipeline(1);
        pipeline.RegisterSystem(new RecordingSystem("a", 10, []));
        pipeline.RegisterSystem(new RecordingSystem("b", 10, []));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(pipeline.Freeze);

        Assert.Contains("order", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FreezeRejectsDuplicateSystemIds()
    {
        var pipeline = CreatePipeline(1);
        pipeline.RegisterSystem(new RecordingSystem("same", 10, []));
        pipeline.RegisterSystem(new RecordingSystem("same", 20, []));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(pipeline.Freeze);

        Assert.Contains("ID", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FreezeRejectsDefaultFrequency()
    {
        var pipeline = CreatePipeline(1);
        pipeline.RegisterSystem(new MutableSystem
        {
            Id = "invalid",
            Frequency = default,
        });

        Assert.Throws<InvalidOperationException>(pipeline.Freeze);
    }

    [Fact]
    public void FreezePreventsLaterRegistrations()
    {
        var pipeline = CreatePipeline(1);
        pipeline.Freeze();

        Assert.Throws<InvalidOperationException>(
            () => pipeline.RegisterSystem(new RecordingSystem("late", 10, [])));
        Assert.Throws<InvalidOperationException>(
            () => pipeline.RegisterStateContributor(new CounterContributor("late", 10)));
    }

    [Fact]
    public void FrequenciesProduceExactExecutionCountsIncludingTickZero()
    {
        var pipeline = CreatePipeline(11);
        pipeline.RegisterSystem(new RecordingSystem("one", 10, []));
        pipeline.RegisterSystem(
            new RecordingSystem("two", 20, [], SimulationSystemFrequency.EveryTwoTicks));
        pipeline.RegisterSystem(
            new RecordingSystem("ten", 30, [], SimulationSystemFrequency.EveryTenTicks));

        SimulationRunResult result = pipeline.Run();

        Assert.Equal(11UL, result.Systems[0].ExecutionCount);
        Assert.Equal(6UL, result.Systems[1].ExecutionCount);
        Assert.Equal(2UL, result.Systems[2].ExecutionCount);
        Assert.Equal(11UL, result.CompletedTicks.Value);
    }

    [Fact]
    public void RunWithoutMaximumRequiresAnExplicitTickCount()
    {
        var pipeline = new SimulationPipeline(
            new SimulationOptions(new DeterministicSeed(9), 60));

        Assert.Throws<InvalidOperationException>(pipeline.Run);

        SimulationRunResult result = pipeline.Run(3);
        Assert.Equal(3UL, result.CompletedTicks.Value);
    }

    [Fact]
    public void RunCanContinueUntilTheConfiguredMaximum()
    {
        var pipeline = CreatePipeline(5);

        pipeline.Run(2);
        SimulationRunResult result = pipeline.Run();

        Assert.Equal(5UL, result.CompletedTicks.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => pipeline.Run(1));
    }

    [Fact]
    public void PipelineSnapshotsSystemSchedulingMetadataWhenFrozen()
    {
        var system = new MutableSystem();
        var pipeline = CreatePipeline(3);
        pipeline.RegisterSystem(system);
        pipeline.Freeze();
        system.Id = "changed";
        system.Order = 999;
        system.Frequency = SimulationSystemFrequency.EveryTenTicks;

        SimulationRunResult result = pipeline.Run();

        SimulationSystemMetrics metrics = Assert.Single(result.Systems);
        Assert.Equal("stable", metrics.SystemId);
        Assert.Equal(10, metrics.Order);
        Assert.Equal(3UL, metrics.ExecutionCount);
    }

    [Fact]
    public void PipelineSnapshotsContributorIdentityAndOrderWhenFrozen()
    {
        var contributor = new MutableContributor();
        var pipeline = CreatePipeline(0);
        pipeline.RegisterStateContributor(contributor);
        pipeline.Freeze();
        SimulationStateHash before = pipeline.ComputeStateHash();
        contributor.Id = "changed";
        contributor.Order = 999;

        SimulationStateHash after = pipeline.ComputeStateHash();

        Assert.Equal(before, after);
    }

    [Fact]
    public void CommandsRunAfterAllSystemsInTheSameTick()
    {
        var trace = new List<string>();
        var pipeline = CreatePipeline(1);
        pipeline.RegisterSystem(new CommandProducingSystem("producer", 10, trace));
        pipeline.RegisterSystem(new RecordingSystem("observer", 20, trace));

        pipeline.Run();

        Assert.Equal(["producer:0", "observer:0", "command:0"], trace);
    }

    [Fact]
    public void NestedCommandRunsDuringTheFollowingPipelineTick()
    {
        var trace = new List<string>();
        var pipeline = CreatePipeline(2);
        pipeline.RegisterSystem(new NestedCommandProducingSystem(trace));

        pipeline.Run();

        Assert.Equal(["parent:0", "child:1"], trace);
    }

    [Fact]
    public void EventsBecomeVisibleOnlyDuringTheFollowingTick()
    {
        var observations = new List<int>();
        var pipeline = CreatePipeline(2);
        pipeline.RegisterSystem(new EventProducingSystem());
        pipeline.RegisterSystem(new EventObservingSystem(observations));

        pipeline.Run();

        Assert.Equal([0, 1], observations);
        TickEvent lastEvent = Assert.IsType<TickEvent>(Assert.Single(pipeline.PublishedEvents));
        Assert.Equal(1UL, lastEvent.Tick);
    }

    [Fact]
    public void AFailedTickDoesNotAdvanceAndPermanentlyFaultsThePipeline()
    {
        var pipeline = CreatePipeline(2);
        pipeline.RegisterSystem(new ThrowingSystem());

        Assert.Throws<TestSimulationException>(pipeline.Run);

        Assert.Equal(0UL, pipeline.CurrentTick.Value);
        Assert.True(pipeline.IsFaulted);
        Assert.False(pipeline.IsTickOpen);
        Assert.Throws<InvalidOperationException>(() => pipeline.Run(1));
    }

    private static SimulationPipeline CreatePipeline(ulong maxTicks) =>
        new(new SimulationOptions(new DeterministicSeed(123), 60, maxTicks));

    private sealed class RecordingSystem : ISimulationSystem
    {
        private readonly ICollection<string> _trace;

        public RecordingSystem(
            string id,
            int order,
            ICollection<string> trace,
            SimulationSystemFrequency? frequency = null)
        {
            Id = id;
            Order = order;
            _trace = trace;
            Frequency = frequency ?? SimulationSystemFrequency.EveryTick;
        }

        public string Id { get; }

        public int Order { get; }

        public SimulationSystemFrequency Frequency { get; }

        public void Execute(ISimulationContext context) =>
            _trace.Add($"{Id}:{context.CurrentTick.Value}");
    }

    private sealed class MutableSystem : ISimulationSystem
    {
        public string Id { get; set; } = "stable";

        public int Order { get; set; } = 10;

        public SimulationSystemFrequency Frequency { get; set; } =
            SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) => ArgumentNullException.ThrowIfNull(context);
    }

    private sealed class CommandProducingSystem(
        string id,
        int order,
        ICollection<string> trace) : ISimulationSystem
    {
        public string Id => id;

        public int Order => order;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            trace.Add($"{Id}:{context.CurrentTick.Value}");
            context.EnqueueCommand(new TickCommand(trace));
        }
    }

    private sealed class TickCommand(ICollection<string> trace) : ISimulationCommand
    {
        public string StableTypeId => "tests.pipeline.tick-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
        }

        public void Execute(ISimulationContext context) =>
            trace.Add($"command:{context.CurrentTick.Value}");
    }

    private sealed class NestedCommandProducingSystem(ICollection<string> trace) : ISimulationSystem
    {
        public string Id => "nested-command-producer";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            if (context.CurrentTick == SimulationTick.Initial)
            {
                context.EnqueueCommand(new ParentCommand(trace));
            }
        }
    }

    private sealed class ParentCommand(ICollection<string> trace) : ISimulationCommand
    {
        public string StableTypeId => "tests.pipeline.parent-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
        }

        public void Execute(ISimulationContext context)
        {
            trace.Add($"parent:{context.CurrentTick.Value}");
            context.EnqueueCommand(new ChildCommand(trace));
        }
    }

    private sealed class ChildCommand(ICollection<string> trace) : ISimulationCommand
    {
        public string StableTypeId => "tests.pipeline.child-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
        }

        public void Execute(ISimulationContext context) =>
            trace.Add($"child:{context.CurrentTick.Value}");
    }

    private sealed class EventProducingSystem : ISimulationSystem
    {
        public string Id => "events.producer";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) =>
            context.PublishEvent(new TickEvent(context.CurrentTick.Value));
    }

    private sealed class EventObservingSystem(ICollection<int> observations) : ISimulationSystem
    {
        public string Id => "events.observer";

        public int Order => 20;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context) => observations.Add(context.Events.Count);
    }

    private sealed class ThrowingSystem : ISimulationSystem
    {
        public string Id => "throws";

        public int Order => 10;

        public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

        public void Execute(ISimulationContext context)
        {
            context.PublishEvent(new TickEvent(context.CurrentTick.Value));
            throw new TestSimulationException();
        }
    }

    private sealed class CounterContributor(string id, int order) : IStateHashContributor
    {
        public string Id => id;

        public int Order => order;

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(0UL);
    }

    private sealed class MutableContributor : IStateHashContributor
    {
        public string Id { get; set; } = "stable.contributor";

        public int Order { get; set; } = 10;

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(42UL);
    }

    private sealed record TickEvent(ulong Tick) : ISimulationEvent
    {
        public string StableTypeId => "tests.pipeline.tick-event.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(Tick);
    }

    private sealed class TestSimulationException : Exception;
}
