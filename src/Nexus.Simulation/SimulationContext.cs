using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// The deliberately small deterministic surface presented to systems and commands.
/// </summary>
public sealed class SimulationContext : ISimulationContext
{
    private readonly SimulationClock _clock;
    private readonly RandomStreamProvider _randomStreams;
    private readonly CommandBuffer _commands;
    private readonly EventBuffer _events;
    private readonly string _randomStreamOwnerId;
    private readonly bool _usesGlobalRandomScope;

    public SimulationContext(
        SimulationClock clock,
        RandomStreamProvider randomStreams,
        CommandBuffer commands,
        EventBuffer events)
        : this(clock, randomStreams, commands, events, string.Empty, true)
    {
    }

    internal SimulationContext(
        SimulationClock clock,
        RandomStreamProvider randomStreams,
        CommandBuffer commands,
        EventBuffer events,
        string randomStreamOwnerId,
        bool usesGlobalRandomScope = false)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(randomStreams);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(events);
        if (!usesGlobalRandomScope)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(randomStreamOwnerId);
        }

        _clock = clock;
        _randomStreams = randomStreams;
        _commands = commands;
        _events = events;
        _randomStreamOwnerId = randomStreamOwnerId;
        _usesGlobalRandomScope = usesGlobalRandomScope;
    }

    public SimulationTick CurrentTick => _clock.CurrentTick;

    public int TickRate => _clock.TickRate;

    public double LogicalTimeSeconds => _clock.LogicalTimeSeconds;

    public double FixedDeltaTimeSeconds => _clock.FixedDeltaTimeSeconds;

    public IReadOnlyList<ISimulationEvent> Events => _events.PublishedEvents;

    internal bool UsesGlobalRandomScope => _usesGlobalRandomScope;

    internal string RandomStreamOwnerId => _randomStreamOwnerId;

    public ISimulationRandomSource GetRandomStream(string key) =>
        _usesGlobalRandomScope
            ? _randomStreams.GetStream(key)
            : _randomStreams.GetSystemStream(_randomStreamOwnerId, key);

    public void EnqueueCommand(ISimulationCommand command) => _commands.Enqueue(command, this);

    public void PublishEvent(ISimulationEvent simulationEvent) => _events.Publish(simulationEvent);
}
