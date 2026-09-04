using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Host;

/// <summary>
/// Demonstrates an independently keyed PRNG stream and an every-ten-ticks cadence.
/// </summary>
internal sealed class RandomAccumulatorSystem : ISimulationSystem, IStateHashContributor
{
    private const string RandomStreamKey = "demo.random-accumulator.values";

    private ulong _accumulator;
    private ulong _sampleCount;

    public string Id => "demo.random-accumulator";

    public int Order => 200;

    public SimulationSystemFrequency Frequency => SimulationSystemFrequency.EveryTenTicks;

    public void Execute(ISimulationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ulong sample = context.GetRandomStream(RandomStreamKey).NextUInt64();
        _accumulator = unchecked((_accumulator * 31UL) + sample);
        _sampleCount = checked(_sampleCount + 1UL);
        context.PublishEvent(new RandomSampledEvent(context.CurrentTick));
    }

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(_accumulator);
        hasher.Add(_sampleCount);
    }
}

internal readonly record struct RandomSampledEvent(SimulationTick Tick) : ISimulationEvent
{
    public string StableTypeId => "nexus.demo.random-sampled-event.v1";

    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(Tick.Value);
    }
}
