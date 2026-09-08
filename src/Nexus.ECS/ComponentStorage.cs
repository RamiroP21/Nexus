using Nexus.Core;

namespace Nexus.ECS;

internal interface IComponentStorage
{
    string StableId { get; }

    int RuntimeId { get; }

    bool Remove(EntityId entity);

    void ApplyPendingAdd(EntityId entity, int payloadIndex);

    void ContributePendingPayload(int payloadIndex, StableHasher64 hasher);

    void ClearPendingPayloads();

    void ContributeToHash(StableHasher64 hasher);
}

internal sealed class ComponentStorage<T>(string stableId, int runtimeId, int capacity)
    : IComponentStorage where T : unmanaged, IComponent<T>
{
    private readonly List<T> _pendingPayloads = [];

    public SparseSet<T> Data { get; } = new(capacity);

    public string StableId { get; } = stableId;

    public int RuntimeId { get; } = runtimeId;

    public int EnqueuePayload(in T value)
    {
        int index = _pendingPayloads.Count;
        _pendingPayloads.Add(value);
        return index;
    }

    public bool Remove(EntityId entity) => Data.Remove(entity);

    public void ApplyPendingAdd(EntityId entity, int payloadIndex) =>
        Data.Add(entity, _pendingPayloads[payloadIndex]);

    public void ContributePendingPayload(int payloadIndex, StableHasher64 hasher) =>
        T.ContributeToHash(_pendingPayloads[payloadIndex], hasher);

    public void ClearPendingPayloads() => _pendingPayloads.Clear();

    public void ContributeToHash(StableHasher64 hasher) => Data.ContributeToHash(hasher);
}
