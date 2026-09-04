using System.Globalization;
using Nexus.Diagnostics;

namespace Nexus.Host;

internal static class Program
{
    private const ulong DefaultSeed = 123_456_789UL;
    private const int DefaultTickRate = 60;
    private const ulong DefaultTickCount = 10_000UL;

    public static int Main()
    {
        DeterminismReport runA = DemoSimulation.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);
        DeterminismReport runB = DemoSimulation.RunFresh(
            DefaultSeed,
            DefaultTickRate,
            DefaultTickCount);

        bool isDeterministic = string.Equals(
            runA.FinalStateHash,
            runB.FinalStateHash,
            StringComparison.Ordinal);

        PrintHeader(runA);
        PrintRun("RUN A", runA);
        PrintRun("RUN B", runB);
        PrintSystemMetrics(runA, runB);

        Console.WriteLine();
        Console.WriteLine(isDeterministic ? "DETERMINISM: PASS" : "DETERMINISM: FAIL");

        return isDeterministic ? 0 : 1;
    }

    private static void PrintHeader(DeterminismReport report)
    {
        Console.WriteLine("NEXUS HEADLESS SIMULATION");
        Console.WriteLine("=========================");
        WriteInvariant($"Seed:      {report.Seed.Value}");
        WriteInvariant($"Tick rate: {report.TickRate} Hz");
        WriteInvariant($"Ticks:     {report.TickCount}");
    }

    private static void PrintRun(string label, DeterminismReport report)
    {
        Console.WriteLine();
        Console.WriteLine(label);
        WriteInvariant($"  State hash: {report.FinalStateHash}");
        WriteInvariant($"  Elapsed:    {report.Elapsed.TotalMilliseconds:F3} ms");

        string speed = report.RealTimeSpeed is double realTimeSpeed
            ? realTimeSpeed.ToString("F2", CultureInfo.InvariantCulture) + "x"
            : "n/a";
        Console.WriteLine($"  Speed:      {speed} real-time");
    }

    private static void PrintSystemMetrics(
        DeterminismReport runA,
        DeterminismReport runB)
    {
        Console.WriteLine();
        Console.WriteLine("SYSTEMS (RUN A / RUN B)");

        for (int i = 0; i < runA.Systems.Count; i++)
        {
            SystemExecutionMetric systemA = runA.Systems[i];
            SystemExecutionMetric systemB = runB.Systems[i];
            WriteInvariant($"  [{systemA.Order}] {systemA.SystemId} (every {systemA.Frequency.Interval} tick(s)): {systemA.ExecutionCount} / {systemB.ExecutionCount}");
        }
    }

    private static void WriteInvariant(FormattableString value) =>
        Console.WriteLine(value.ToString(CultureInfo.InvariantCulture));
}
