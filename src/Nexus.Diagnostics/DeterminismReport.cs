using System.Collections.ObjectModel;
using Nexus.Core;

namespace Nexus.Diagnostics;

/// <summary>
/// Immutable diagnostics captured outside deterministic simulation state.
/// </summary>
public sealed class DeterminismReport
{
    private readonly ReadOnlyCollection<SystemExecutionMetric> _systems;

    public DeterminismReport(
        DeterministicSeed seed,
        int tickRate,
        ulong tickCount,
        IEnumerable<SystemExecutionMetric> systems,
        string finalStateHash,
        TimeSpan elapsed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickRate);
        ArgumentNullException.ThrowIfNull(systems);
        ArgumentException.ThrowIfNullOrWhiteSpace(finalStateHash);
        ArgumentOutOfRangeException.ThrowIfLessThan(elapsed, TimeSpan.Zero);

        var systemArray = systems.ToArray();
        EnsureUniqueSystemIds(systemArray);

        Seed = seed;
        TickRate = tickRate;
        TickCount = tickCount;
        _systems = Array.AsReadOnly(systemArray);
        FinalStateHash = finalStateHash;
        Elapsed = elapsed;
    }

    public DeterministicSeed Seed { get; }

    public int TickRate { get; }

    public ulong TickCount { get; }

    public IReadOnlyList<SystemExecutionMetric> Systems => _systems;

    public string FinalStateHash { get; }

    public TimeSpan Elapsed { get; }

    public double SimulatedDurationSeconds => TickCount / (double)TickRate;

    public double? RealTimeSpeed => Elapsed > TimeSpan.Zero
        ? SimulatedDurationSeconds / Elapsed.TotalSeconds
        : null;

    private static void EnsureUniqueSystemIds(IEnumerable<SystemExecutionMetric> systems)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var system in systems)
        {
            if (string.IsNullOrWhiteSpace(system.SystemId))
            {
                throw new ArgumentException(
                    "Every system diagnostic requires a stable ID.",
                    nameof(systems));
            }

            if (!ids.Add(system.SystemId))
            {
                throw new ArgumentException(
                    $"System diagnostics contain duplicate id '{system.SystemId}'.",
                    nameof(systems));
            }
        }
    }
}
