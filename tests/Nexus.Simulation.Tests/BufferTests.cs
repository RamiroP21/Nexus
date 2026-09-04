using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation.Tests;

public sealed class BufferTests
{
    [Fact]
    public void CommandBufferProcessesCommandsInFifoOrder()
    {
        var trace = new List<string>();
        SimulationContext context = CreateContext(out CommandBuffer commands, out EventBuffer events);
        commands.Enqueue(new TraceCommand("first", trace));
        commands.Enqueue(new TraceCommand("second", trace));
        events.BeginTick();

        int processed = commands.ProcessPending(context);
        events.CompleteTick();

        Assert.Equal(2, processed);
        Assert.Equal(["first", "second"], trace);
        Assert.Equal(0, commands.Count);
    }

    [Fact]
    public void CommandCreatedDuringCommandPhaseWaitsUntilNextPhase()
    {
        var trace = new List<string>();
        SimulationContext context = CreateContext(out CommandBuffer commands, out EventBuffer events);
        commands.Enqueue(new EnqueueingCommand(trace));
        events.BeginTick();

        Assert.Equal(1, commands.ProcessPending(context));

        Assert.Equal(["parent"], trace);
        Assert.Equal(1, commands.Count);
        Assert.Equal(1, commands.ProcessPending(context));
        Assert.Equal(["parent", "child"], trace);
        events.CompleteTick();
    }

    [Fact]
    public void EventBufferPublishesAnImmutableOrderedBatchAtTickCompletion()
    {
        var events = new EventBuffer();
        events.BeginTick();
        events.Publish(new TestEvent(1));
        events.Publish(new TestEvent(2));

        Assert.Empty(events.PublishedEvents);

        events.CompleteTick();

        Assert.Equal([1, 2], events.PublishedEvents.Cast<TestEvent>().Select(static item => item.Value));
        Assert.IsAssignableFrom<IReadOnlyList<ISimulationEvent>>(events.PublishedEvents);
    }

    [Fact]
    public void EventBufferRequiresAnExplicitOpenTick()
    {
        var events = new EventBuffer();

        Assert.Throws<InvalidOperationException>(() => events.Publish(new TestEvent(1)));
        Assert.Throws<InvalidOperationException>(events.CompleteTick);
    }

    [Fact]
    public void AbortingATickDiscardsOnlyItsUnpublishedEvents()
    {
        var events = new EventBuffer();
        events.BeginTick();
        events.Publish(new TestEvent(1));
        events.CompleteTick();
        events.BeginTick();
        events.Publish(new TestEvent(2));

        events.AbortTick();

        TestEvent published = Assert.IsType<TestEvent>(Assert.Single(events.PublishedEvents));
        Assert.Equal(1, published.Value);
        Assert.False(events.IsTickOpen);
    }

    private static SimulationContext CreateContext(
        out CommandBuffer commands,
        out EventBuffer events)
    {
        commands = new CommandBuffer();
        events = new EventBuffer();
        return new SimulationContext(
            new SimulationClock(60),
            new RandomStreamProvider(new DeterministicSeed(7)),
            commands,
            events);
    }

    private sealed record TestEvent(int Value) : ISimulationEvent
    {
        public string StableTypeId => "tests.buffer.test-event.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(Value);
    }

    private sealed class TraceCommand(string value, ICollection<string> trace) : ISimulationCommand
    {
        public string StableTypeId => "tests.buffer.trace-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add(value);

        public void Execute(ISimulationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            trace.Add(value);
        }
    }

    private sealed class EnqueueingCommand(ICollection<string> trace) : ISimulationCommand
    {
        public string StableTypeId => "tests.buffer.enqueueing-command.v1";

        public void ContributeToHash(StableHasher64 hasher) => hasher.Add("child");

        public void Execute(ISimulationContext context)
        {
            trace.Add("parent");
            context.EnqueueCommand(new TraceCommand("child", trace));
        }
    }
}
