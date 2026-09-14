using System.Globalization;
using System.Text.Json;
using Nexus.Simulation;
using Nexus.Diagnostics;

namespace Nexus.Host;

internal static class Program
{
    private static readonly JsonSerializerOptions ReplayJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    private const ulong DefaultSeed = 123_456_789UL;
    private const int DefaultTickRate = 60;
    private const ulong DefaultTickCount = 10_000UL;

    public static int Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--district-host", StringComparison.Ordinal))
        {
            int port = args.Length > 1 && int.TryParse(args[1], out int parsedPort) ? parsedPort : 0;
            int directoryIndex = Array.IndexOf(args, "--save-directory");
            string saveDirectory = directoryIndex >= 0 && directoryIndex + 1 < args.Length ? args[directoryIndex + 1] : CampaignSaveRepository.DefaultDirectory;
            return RunDistrictHost(port, saveDirectory);
        }
        if (args.Length > 1 && string.Equals(args[0], "--replay", StringComparison.Ordinal))
            return RunReplay(args[1]);
        if (args.Length > 0)
        {
            return EcsDemoConsole.Run(args);
        }

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

    private static int RunDistrictHost(int port, string saveDirectory)
    {
        using var stop = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; stop.Cancel(); };
        var host = new DistrictAuthorityHost(port, saveDirectory);
        try
        {
            Task run = host.RunAsync(stop.Token);
            while (!run.Wait(100)) { }
            run.GetAwaiter().GetResult();
            return 0;
        }
        catch (OperationCanceledException) { return 0; }
        finally { host.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
    }

    private static int RunReplay(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            DistrictReplayDocument? replay = JsonSerializer.Deserialize<DistrictReplayDocument>(json, ReplayJsonOptions);
            if (replay is null) throw new InvalidDataException("Replay is empty.");
            DistrictAuthoritySession session = DistrictReplay.Run(replay);
            Console.WriteLine($"REPLAY: PASS");
            Console.WriteLine($"Seed: {session.Seed}");
            Console.WriteLine($"Commands: {replay.Commands.Count}");
            Console.WriteLine($"Final tick: {session.Tick}");
            Console.WriteLine($"Final authoritative hash: {session.ComputeStateHash()}");
            return 0;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
        {
            Console.Error.WriteLine($"REPLAY: FAIL ({ex.Message})");
            return 1;
        }
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
