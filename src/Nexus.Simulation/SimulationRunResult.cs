using Nexus.Core;

namespace Nexus.Simulation;

public sealed class SimulationRunResult
{
    internal SimulationRunResult(
        DeterministicSeed seed,
        int tickRate,
        SimulationTick completedTicks,
        SimulationStateHash stateHash,
        IReadOnlyList<SimulationSystemMetrics> systems)
    {
        Seed = seed;
        TickRate = tickRate;
        CompletedTicks = completedTicks;
        StateHash = stateHash.Value;
        StateHashHex = stateHash.Hex;
        Systems = systems;
    }

    public DeterministicSeed Seed { get; }

    public int TickRate { get; }

    public SimulationTick CompletedTicks { get; }

    public ulong StateHash { get; }

    public string StateHashHex { get; }

    public IReadOnlyList<SimulationSystemMetrics> Systems { get; }
}
