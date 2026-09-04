namespace Nexus.Contracts;

/// <summary>
/// A sequential pipeline stage with stable identity, order, and cadence.
/// </summary>
public interface ISimulationSystem
{
    string Id { get; }

    int Order { get; }

    SimulationSystemFrequency Frequency { get; }

    void Execute(ISimulationContext context);
}
