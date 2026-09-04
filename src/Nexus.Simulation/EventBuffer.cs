using Nexus.Contracts;
using Nexus.Core;

namespace Nexus.Simulation;

/// <summary>
/// Collects events during a tick and exposes the completed batch to the following tick.
/// </summary>
public sealed class EventBuffer
{
    private const string HashFormatVersion = "Nexus.Simulation.EventBuffer.v1";
    private const string EventHashFormatVersion = "Nexus.Simulation.Event.v1";

    private readonly List<BufferedEvent> _emittedThisTick = [];
    private IReadOnlyList<BufferedEvent> _publishedEntries = Array.Empty<BufferedEvent>();
    private IReadOnlyList<ISimulationEvent> _publishedEvents = Array.Empty<ISimulationEvent>();

    public IReadOnlyList<ISimulationEvent> PublishedEvents => _publishedEvents;

    public bool IsTickOpen { get; private set; }

    public void BeginTick()
    {
        if (IsTickOpen)
        {
            throw new InvalidOperationException("An event-buffer tick is already open.");
        }

        IsTickOpen = true;
    }

    public void Publish(ISimulationEvent simulationEvent)
    {
        ArgumentNullException.ThrowIfNull(simulationEvent);
        EnsureTickOpen();
        _emittedThisTick.Add(new BufferedEvent(
            simulationEvent,
            StateHashablePayload.CaptureStableTypeId(simulationEvent)));
    }

    public void CompleteTick()
    {
        EnsureTickOpen();
        BufferedEvent[] completedBatch = _emittedThisTick.ToArray();
        _publishedEntries = Array.AsReadOnly(completedBatch);
        _publishedEvents = Array.AsReadOnly(
            completedBatch.Select(static buffered => buffered.Event).ToArray());
        _emittedThisTick.Clear();
        IsTickOpen = false;
    }

    public void AbortTick()
    {
        EnsureTickOpen();
        _emittedThisTick.Clear();
        IsTickOpen = false;
    }

    internal void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        if (IsTickOpen)
        {
            throw new InvalidOperationException(
                "An event buffer can only be hashed at a completed tick boundary.");
        }

        hasher.Add(HashFormatVersion);
        ContributeBatchToHash(hasher, "published", _publishedEntries);
    }

    private void EnsureTickOpen()
    {
        if (!IsTickOpen)
        {
            throw new InvalidOperationException("No event-buffer tick is open.");
        }
    }

    private static void ContributeBatchToHash(
        StableHasher64 hasher,
        string batchId,
        IReadOnlyCollection<BufferedEvent> events)
    {
        hasher.Add(batchId);
        hasher.Add(checked((ulong)events.Count));

        ulong index = 0;
        foreach (BufferedEvent buffered in events)
        {
            hasher.Add(index);
            hasher.Add(StateHashablePayload.ComputeHash(
                EventHashFormatVersion,
                buffered.StableTypeId,
                buffered.Event));
            index++;
        }
    }

    private sealed record BufferedEvent(ISimulationEvent Event, string StableTypeId);
}
