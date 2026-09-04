namespace Nexus.Core;

/// <summary>
/// A deterministic PCG-XSH-RR 64/32 pseudo-random number generator.
/// </summary>
/// <remarks>
/// Instances are mutable and intentionally not thread-safe. Each deterministic owner must use its own instance.
/// </remarks>
public sealed class DeterministicRandom
{
    private const ulong Multiplier = 6_364_136_223_846_793_005UL;
    private const ulong DefaultStream = 54UL;

    internal const ulong MaximumStream = 0x7FFF_FFFF_FFFF_FFFFUL;

    private ulong _state;
    private readonly ulong _increment;

    /// <summary>
    /// Initializes a generator using the reference PCG seeding procedure and the default stream selector 54.
    /// </summary>
    public DeterministicRandom(DeterministicSeed seed)
        : this(seed, DefaultStream)
    {
    }

    /// <summary>
    /// Initializes a generator using the reference PCG seeding procedure and the supplied stream selector.
    /// </summary>
    /// <remarks>
    /// PCG uses 63 stream-selection bits. The most significant bit of <paramref name="stream"/> is ignored.
    /// </remarks>
    public DeterministicRandom(DeterministicSeed seed, ulong stream)
    {
        Stream = stream & MaximumStream;
        _increment = unchecked((Stream << 1) | 1UL);

        _ = NextUInt32();
        _state = unchecked(_state + seed.Value);
        _ = NextUInt32();
    }

    /// <summary>
    /// Gets the current reproducible 64-bit PCG state.
    /// </summary>
    public ulong State => _state;

    /// <summary>
    /// Gets the effective 63-bit PCG stream selector.
    /// </summary>
    public ulong Stream { get; }

    /// <summary>
    /// Creates a generator associated with a stable stream key.
    /// </summary>
    public static DeterministicRandom ForStream(DeterministicSeed rootSeed, string key) =>
        DeterministicSeedDerivation.CreateRandom(rootSeed, key);

    /// <summary>
    /// Produces the next value from the PCG32 sequence.
    /// </summary>
    public uint NextUInt32()
    {
        ulong previousState = _state;
        _state = unchecked((previousState * Multiplier) + _increment);

        uint xorShifted = (uint)(((previousState >> 18) ^ previousState) >> 27);
        int rotation = (int)(previousState >> 59);
        return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
    }

    /// <summary>
    /// Produces the next 64-bit value by placing the first PCG32 draw in the high half.
    /// </summary>
    public ulong NextUInt64() => ((ulong)NextUInt32() << 32) | NextUInt32();

    /// <summary>
    /// Produces an integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) using rejection sampling.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxExclusive"/> is not greater than <paramref name="minInclusive"/>.
    /// </exception>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                maxExclusive,
                "The exclusive maximum must be greater than the inclusive minimum.");
        }

        uint range = checked((uint)((long)maxExclusive - minInclusive));
        uint threshold = unchecked(0U - range) % range;

        uint sample;
        do
        {
            sample = NextUInt32();
        }
        while (sample < threshold);

        return checked((int)(minInclusive + (long)(sample % range)));
    }

    /// <summary>
    /// Produces a value in [0, 1) from the high 53 bits of one explicitly composed 64-bit draw.
    /// </summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / 9_007_199_254_740_992.0);

    /// <summary>
    /// Adds all state that determines future output to a stable hash.
    /// </summary>
    public void ContributeToHash(StableHasher64 hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        hasher.Add(_state);
        hasher.Add(Stream);
    }
}
