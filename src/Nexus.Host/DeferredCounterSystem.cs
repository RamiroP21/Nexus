using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Host;

/// <summary>
/// Demonstrates FIFO command application without representing gameplay.
/// </summary>
internal sealed class DeferredCounterSystem : ISimulationSystem, IStateHashContributor
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
            if (simulationEvent is RandomSampledEvent)
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

    private sealed class IncrementCommand(DeferredCounterSystem owner) : ISimulationCommand
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
