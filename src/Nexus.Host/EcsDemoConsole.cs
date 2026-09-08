using System.Globalization;

namespace Nexus.Host;

internal static class EcsDemoConsole
{
    public static int Run(string[] args)
    {
        if (!TryParse(args, out int entityCount, out ulong tickCount))
        {
            Console.Error.WriteLine("Usage: Nexus.Host [--ecs [--entities <positive integer>] [--ticks <integer >= 5>]]");
            return 2;
        }

        EcsDemoResult runA = EcsDemoSimulation.RunFresh(entityCount, tickCount);
        EcsDemoResult runB = EcsDemoSimulation.RunFresh(entityCount, tickCount);
        bool deterministic = string.Equals(runA.FinalHash, runB.FinalHash, StringComparison.Ordinal)
            && string.Equals(runA.PendingHash, runB.PendingHash, StringComparison.Ordinal);
        bool passed = deterministic && runA.PendingHashChanged && runB.PendingHashChanged
            && runA.LifecyclePassed && runB.LifecyclePassed;

        Console.WriteLine("NEXUS ECS FOUNDATION DEMO");
        WriteInvariant($"Entities: {entityCount}; ticks: {tickCount}; seed: 123456789; rate: 60 Hz");
        PrintRun("RUN A", runA);
        PrintRun("RUN B", runB);
        Console.WriteLine();
        Console.WriteLine("Timing and allocation metrics are diagnostic only; simulation timing includes final hashing.");
        Console.WriteLine("Heap delta is approximate (without forced GC); query metrics cover numeric updates only.");
        Console.WriteLine(deterministic ? "ECS DETERMINISM: PASS" : "ECS DETERMINISM: FAIL");
        Console.WriteLine(passed ? "ECS SCENARIO: PASS" : "ECS SCENARIO: FAIL");
        return passed ? 0 : 1;
    }

    private static void PrintRun(string label, EcsDemoResult run)
    {
        Console.WriteLine();
        Console.WriteLine(label);
        WriteInvariant($"  State hash: {run.FinalHash}");
        WriteInvariant($"  Pending hash: {run.PendingHash}; pending changes hash: {run.PendingHashChanged}");
        WriteInvariant($"  Alive: {run.AliveEntities}; components: {run.ComponentCount}; fidelity: all Full");
        WriteInvariant($"  Destroyed: {run.DestroyedEntities}; reused: {run.ReusedSlots}; observed fidelity events: {run.ObservedFidelityEvents}");
        WriteInvariant($"  Creation: {run.CreationElapsed.TotalMilliseconds:F3} ms; query updates: {run.QueryElapsed.TotalMilliseconds:F3} ms ({run.QueryRows} rows)");
        WriteInvariant($"  Simulation: {run.SimulationElapsed.TotalMilliseconds:F3} ms");
        WriteInvariant($"  Allocated bytes: creation={run.CreationAllocatedBytes}; query updates={run.QueryAllocatedBytes}; simulation={run.SimulationAllocatedBytes}");
        WriteInvariant($"  Approximate heap delta: {run.ApproximateHeapDelta} bytes");
    }

    private static bool TryParse(string[] args, out int entityCount, out ulong tickCount)
    {
        entityCount = 100_000;
        tickCount = 100;
        if (args.Length == 0 || !string.Equals(args[0], "--ecs", StringComparison.Ordinal))
        {
            return false;
        }

        bool hasEntities = false;
        bool hasTicks = false;
        for (int i = 1; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length)
            {
                return false;
            }

            if (string.Equals(args[i], "--entities", StringComparison.Ordinal) && !hasEntities)
            {
                if (!int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out entityCount)
                    || entityCount <= 0 || entityCount == int.MaxValue)
                {
                    return false;
                }

                hasEntities = true;
            }
            else if (string.Equals(args[i], "--ticks", StringComparison.Ordinal) && !hasTicks)
            {
                if (!ulong.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out tickCount)
                    || tickCount < 5)
                {
                    return false;
                }

                hasTicks = true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteInvariant(FormattableString value) =>
        Console.WriteLine(value.ToString(CultureInfo.InvariantCulture));
}
