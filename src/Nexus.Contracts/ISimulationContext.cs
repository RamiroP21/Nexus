using Nexus.Core;

namespace Nexus.Contracts;

/// <summary>
/// Provides the deterministic, tick-scoped capabilities required by a simulation system.
/// </summary>
public interface ISimulationContext
{
    SimulationTick CurrentTick { get; }

    int TickRate { get; }

    double LogicalTimeSeconds { get; }

    double FixedDeltaTimeSeconds { get; }

    IReadOnlyList<ISimulationEvent> Events { get; }

    ISimulationRandomSource GetRandomStream(string key);

    void EnqueueCommand(ISimulationCommand command);

    void PublishEvent(ISimulationEvent simulationEvent);
}
