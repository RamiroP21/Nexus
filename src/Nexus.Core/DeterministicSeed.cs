using System.Globalization;

namespace Nexus.Core;

/// <summary>
/// The stable, 64-bit root of deterministic random generation.
/// </summary>
public readonly record struct DeterministicSeed(ulong Value)
{
    /// <summary>
    /// Derives a stable child seed for <paramref name="key"/>.
    /// </summary>
    public DeterministicSeed Derive(string key) => DeterministicSeedDerivation.Derive(this, key);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
