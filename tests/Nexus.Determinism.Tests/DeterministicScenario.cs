using Nexus.Contracts;
using Nexus.Core;
using Nexus.Simulation;

namespace Nexus.Determinism.Tests;

internal static class DeterministicScenario
{
    internal static ScenarioResult RunFresh(ulong seed, int tickRate, ulong tickCount)
    {
        var counterSystem = new ScenarioDeferredCounterSystem();
        var accumulatorSystem = new ScenarioRandomAccumulatorSystem();
        var options = new SimulationOptions(new DeterministicSeed(seed), tickRate, tickCount);

        var pipeline = new SimulationPipeline(options)
            .RegisterSystem(counterSystem)
            .RegisterSystem(accumulatorSystem)
            .RegisterStateContributor(counterSystem)
            .RegisterStateContributor(accumulatorSystem);

        SimulationRunResult result = pipeline.Run();
        return new ScenarioResult(result, accumulatorSystem.Accumulator);
    }
}

internal sealed record ScenarioResult(
    SimulationRunResult RunResult,
    ulong RandomAccumulator);

internal sealed class ScenarioDeferredCounterSystem : ISimulationSystem, IStateHashContributor
{
    private const string SystemId = "demo.deferred-counter";

    private ulong _counter;
    private ulong _observedSampleEvents;

    public string Id => SystemId;

    public int Order => 100;

    public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTick;

    public void Execute(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (ISimulationEvent simulationEvent in context.Events)
        {
            if (simulationEvent is ScenarioRandomSampledEvent)
            {
                _observedSampleEvents = checked(_observedSampleEvents + 1UL);
            }
        }

        context.EnqueueCommand(new IncrementCommand(this));
    }

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(_counter);
        hasher.Add(_observedSampleEvents);
    }

    private void Increment() => _counter = checked(_counter + 1UL);

    private sealed class IncrementCommand(ScenarioDeferredCounterSystem owner) : ISimulationCommand
    {
        public string StableTypeId => "nexus.demo.deferred-counter.increment-command.v1";

        public void ContributeToHash(StableHasher64 hasher)
        {
            ArgumentNullException.ThrowIfNull(hasher);
            hasher.Add(SystemId);
        }

        public void Execute(ISimulationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            owner.Increment();
        }
    }
}

internal sealed class ScenarioRandomAccumulatorSystem : ISimulationSystem, IStateHashContributor
{
    private const string RandomStreamKey = "demo.random-accumulator.values";

    private ulong _sampleCount;

    public string Id => "demo.random-accumulator";

    public int Order => 200;

    public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTenTicks;

    public ulong Accumulator { get; private set; }

    public void Execute(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ulong sample = context.GetRandomStream(RandomStreamKey).NextUInt64();
        Accumulator = unchecked((Accumulator * 31UL) + sample);
        _sampleCount = checked(_sampleCount + 1UL);
        context.PublishEvent(new ScenarioRandomSampledEvent(context.CurrentTick));
    }

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(Accumulator);
        hasher.Add(_sampleCount);
    }
}

internal readonly record struct ScenarioRandomSampledEvent(SimulationTick Tick) : ISimulationEvent
{
    public string StableTypeId => "nexus.demo.random-sampled-event.v1";

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(Tick.Value);
    }
}
