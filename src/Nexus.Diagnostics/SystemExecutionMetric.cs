using Nexus.Contracts;

namespace Nexus.Diagnostics;

/// <summary>
/// Describes how often one stable simulation system executed during a run.
/// </summary>
public readonly record struct SystemExecutionMetric
{
    public SystemExecutionMetric(
        string systemId,
        int order,
        SimulationSystemFrequency frequency,
        ulong executionCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);

        if (frequency.Interval == 0)
        {
            throw new ArgumentException("System frequency must be initialized.", nameof(frequency));
        }

        SystemId = systemId;
        Order = order;
        Frequency = frequency;
        ExecutionCount = executionCount;
    }

    public string SystemId { get; }

    public int Order { get; }

    public SimulationSystemFrequency Frequency { get; }

    public ulong ExecutionCount { get; }
}
