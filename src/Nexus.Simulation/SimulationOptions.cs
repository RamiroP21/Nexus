using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// Immutable configuration for one simulation instance.
/// </summary>
public sealed class SimulationOptions
{
    public SimulationOptions(DeterministicSeed seed, int tickRate, ulong? maxTicks = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickRate);

        Seed = seed;
        TickRate = tickRate;
        MaxTicks = maxTicks;
    }

    public DeterministicSeed Seed { get; }

    public int TickRate { get; }

    public ulong? MaxTicks { get; }
}
