using Nexus.Contracts;
using Nexus.Core;
using System.Globalization;
using System.Text;

namespace Nexus.Simulation;

/// <summary>
/// Lazily creates independent deterministic streams derived solely from a root seed and key.
/// </summary>
public sealed class RandomStreamProvider
{
    private readonly Dictionary<string, SimulationRandomStream> _streams =
        new(StringComparer.Ordinal);
    private readonly DeterministicSeed _rootSeed;

    public RandomStreamProvider(DeterministicSeed rootSeed) => _rootSeed = rootSeed;

    public ISimulationRandomSource GetStream(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return GetOrCreate(CreateCanonicalKey("global", string.Empty, key));
    }

    internal ISimulationRandomSource GetSystemStream(string systemId, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return GetOrCreate(CreateCanonicalKey("system", systemId, key));
    }

    private SimulationRandomStream GetOrCreate(string canonicalKey)
    {
        if (_streams.TryGetValue(canonicalKey, out SimulationRandomStream? stream))
        {
            return stream;
        }

        stream = new SimulationRandomStream(
            DeterministicSeedDerivation.CreateRandom(_rootSeed, canonicalKey));
        _streams.Add(canonicalKey, stream);
        return stream;
    }

    internal void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add("Nexus.Simulation.RandomStreams.v1");
        hasher.Add(checked((ulong)_streams.Count));

        foreach (KeyValuePair<string, SimulationRandomStream> stream in
                 _streams.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            hasher.Add(stream.Key);
            stream.Value.ContributeToHash(hasher);
        }
    }

    private static string CreateCanonicalKey(string scopeKind, string ownerId, string key)
    {
        string kindLength = Encoding.UTF8.GetByteCount(scopeKind)
            .ToString(CultureInfo.InvariantCulture);
        string ownerLength = Encoding.UTF8.GetByteCount(ownerId)
            .ToString(CultureInfo.InvariantCulture);
        string keyLength = Encoding.UTF8.GetByteCount(key)
            .ToString(CultureInfo.InvariantCulture);
        return $"Nexus.Simulation.RandomStream.v1|{kindLength}:{scopeKind}|{ownerLength}:{ownerId}|{keyLength}:{key}";
    }

    private sealed class SimulationRandomStream : ISimulationRandomSource
    {
        private readonly DeterministicRandom _random;

        public SimulationRandomStream(DeterministicRandom random) => _random = random;

        public uint NextUInt32() => _random.NextUInt32();

        public ulong NextUInt64() => _random.NextUInt64();

        public int NextInt(int minInclusive, int maxExclusive) =>
            _random.NextInt(minInclusive, maxExclusive);

        public double NextDouble() => _random.NextDouble();

        public void ContributeToHash(StableHasher64 hasher) => _random.ContributeToHash(hasher);
    }
}
