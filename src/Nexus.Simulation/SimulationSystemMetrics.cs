using Nexus.Contracts;

namespace Nexus.Simulation;

public sealed record SimulationSystemMetrics(
    string SystemId,
    int Order,
    SimulationSystemFrequency Frequency,
    ulong ExecutionCount);
