namespace Nexus.Core;

/// <summary>
/// Derives deterministic seeds and PCG stream selectors with FNV-1a 64 over a canonical byte sequence.
/// </summary>
public static class DeterministicSeedDerivation
{
    private const string SeedDomain = "Nexus.Seed.v1\0";
    private const string StreamDomain = "Nexus.Stream.v1\0";

    /// <summary>
    /// Derives a child seed from a root seed and UTF-8 key.
    /// </summary>
    public static DeterministicSeed Derive(DeterministicSeed rootSeed, string key) =>
        new(DeriveValue(rootSeed, key, SeedDomain));

    /// <summary>
    /// Derives the 63-bit selector of an independent PCG stream.
    /// </summary>
    public static ulong DeriveStream(DeterministicSeed rootSeed, string key) =>
        DeriveValue(rootSeed, key, StreamDomain) & DeterministicRandom.MaximumStream;

    /// <summary>
    /// Creates a new deterministic random stream associated only with the root seed and key.
    /// </summary>
    public static DeterministicRandom CreateRandom(DeterministicSeed rootSeed, string key) =>
        new(Derive(rootSeed, key), DeriveStream(rootSeed, key));

    private static ulong DeriveValue(DeterministicSeed rootSeed, string key, string domain)
    {
        ArgumentNullException.ThrowIfNull(key);

        byte[] domainBytes = Fnv1a64.StrictUtf8.GetBytes(domain);
        byte[] keyBytes = Fnv1a64.StrictUtf8.GetBytes(key);

        ulong hash = Fnv1a64.Append(Fnv1a64.OffsetBasis, domainBytes);
        hash = Fnv1a64.AppendUInt64LittleEndian(hash, rootSeed.Value);
        hash = Fnv1a64.AppendUInt64LittleEndian(hash, checked((ulong)keyBytes.LongLength));
        return Fnv1a64.Append(hash, keyBytes);
    }
}
