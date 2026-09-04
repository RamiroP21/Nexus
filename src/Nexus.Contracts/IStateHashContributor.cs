using Nexus.Core;

namespace Nexus.Contracts;

/// <summary>
/// Adds one explicitly ordered state partition to the global deterministic hash.
/// </summary>
public interface IStateHashContributor
{
    string Id { get; }

    int Order { get; }

    void ContributeToHash(StableHasher64 hasher);
}
