using Nexus.Core;
using Nexus.ECS;

namespace Nexus.Host;

internal struct DemoValue(long value) : IComponent<DemoValue>
{
    public long Value = value;

    public static string StableId => "nexus.demo.ecs.value.v1";

    public static void ContributeToHash(in DemoValue value, StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(value.Value);
    }
}

internal readonly struct DemoDelta(long value) : IComponent<DemoDelta>
{
    public long Value { get; } = value;

    public static string StableId => "nexus.demo.ecs.delta.v1";

    public static void ContributeToHash(in DemoDelta value, StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(value.Value);
    }
}
