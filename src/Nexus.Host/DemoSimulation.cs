using System.Diagnostics;
using Nexus.Core;
using Nexus.Diagnostics;
using Nexus.Simulation;

namespace Nexus.Host;

internal static class DemoSimulation
{
    public static DeterminismReport RunFresh(ulong seed, int tickRate, ulong tickCount)
    {
        var options = new SimulationOptions(new DeterministicSeed(seed), tickRate, tickCount);
        var counterSystem = new DeferredCounterSystem();
        var accumulatorSystem = new RandomAccumulatorSystem();

        var pipeline = new SimulationPipeline(options)
            .RegisterSystem(counterSystem)
            .RegisterSystem(accumulatorSystem)
            .RegisterStateContributor(counterSystem)
            .RegisterStateContributor(accumulatorSystem);

        pipeline.Freeze();

        var stopwatch = Stopwatch.StartNew();
        SimulationRunResult result = pipeline.Run();
        stopwatch.Stop();

        IEnumerable<SystemExecutionMetric> systems = result.Systems.Select(
            static system => new SystemExecutionMetric(
                system.SystemId,
                system.Order,
                system.Frequency,
                system.ExecutionCount));

        return new DeterminismReport(
            result.Seed,
            result.TickRate,
            result.CompletedTicks.Value,
            systems,
            result.StateHashHex,
            stopwatch.Elapsed);
    }
}
