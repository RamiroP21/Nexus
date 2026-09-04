using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// A logical clock whose sole source of truth is an integer tick.
/// </summary>
public sealed class SimulationClock
{
    public SimulationClock(int tickRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickRate);
        TickRate = tickRate;
        CurrentTick = SimulationTick.Initial;
    }

    public SimulationTick CurrentTick { get; private set; }

    public int TickRate { get; }

    public double LogicalTimeSeconds => CurrentTick.Value / (double)TickRate;

    public double FixedDeltaTimeSeconds => 1d / TickRate;

    public void Advance() => CurrentTick = CurrentTick.Next();
}
