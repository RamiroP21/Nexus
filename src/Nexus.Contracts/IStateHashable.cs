using Nexus.Core;

namespace Nexus.Contracts;

/// <summary>
/// Provides an explicit, canonical representation of a deterministic payload.
/// </summary>
/// <remarks>
/// <see cref="StableTypeId"/> is a persistent protocol identifier, not a CLR type name. It must
/// uniquely identify one semantic payload schema; changing that schema requires a new identifier.
/// Implementations must contribute every payload field that can affect future simulation behavior.
/// Contributions must be deterministic and free of side effects.
/// </remarks>
public interface IStateHashable
{
    string StableTypeId { get; }

    void ContributeToHash(StableHasher64 hasher);
}
