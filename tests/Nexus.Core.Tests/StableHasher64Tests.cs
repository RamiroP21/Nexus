namespace Nexus.Core.Tests;

public sealed class StableHasher64Tests
{
    [Fact]
    public void EmptyHasherStartsAtFnvOffsetBasis()
    {
        var hasher = new StableHasher64();

        Assert.Equal(14_695_981_039_346_656_037UL, hasher.Value);
        Assert.Equal("CBF29CE484222325", hasher.ToHexString());
        Assert.Equal(hasher.ToHexString(), hasher.ToString());
    }

    [Fact]
    public void AllSupportedTypesMatchFrozenCanonicalVector()
    {
        var hasher = new StableHasher64();

        hasher.Add(true);
        hasher.Add((byte)0xA5);
        hasher.Add(-123_456_789);
        hasher.Add(0xDEADBEEFU);
        hasher.Add(-81_985_529_216_486_895L);
        hasher.Add(0xFEDCBA9876543210UL);
        hasher.Add(-123.5);
        hasher.Add("Nexus ñ☃");
        hasher.Add(new SimulationTick(9_876_543_210));
        hasher.Add(new EntityId(42));
        hasher.Add(new DeterministicSeed(123_456_789));

        Assert.Equal(0x70822922AEB07F4EUL, hasher.Value);
        Assert.Equal("70822922AEB07F4E", hasher.ToHexString());
    }

    [Fact]
    public void TypeTagsDistinguishEqualLookingBinaryPayloads()
    {
        var byteHasher = new StableHasher64();
        var unsignedHasher = new StableHasher64();
        var tickHasher = new StableHasher64();
        var entityHasher = new StableHasher64();
        var seedHasher = new StableHasher64();

        byteHasher.Add((byte)1);
        unsignedHasher.Add(1U);
        tickHasher.Add(new SimulationTick(1));
        entityHasher.Add(new EntityId(1));
        seedHasher.Add(new DeterministicSeed(1));

        ulong[] values =
        [
            byteHasher.Value,
            unsignedHasher.Value,
            tickHasher.Value,
            entityHasher.Value,
            seedHasher.Value,
        ];

        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void StringLengthFramingDistinguishesDifferentValueBoundaries()
    {
        var first = new StableHasher64();
        first.Add("ab");
        first.Add("c");

        var second = new StableHasher64();
        second.Add("a");
        second.Add("bc");

        Assert.Equal("99800F8FCEF199FC", first.ToHexString());
        Assert.Equal("F50EFA17D0050372", second.ToHexString());
        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void DoubleHashingPreservesExactIeeeBits()
    {
        var positiveZero = new StableHasher64();
        positiveZero.Add(0.0);

        var negativeZero = new StableHasher64();
        negativeZero.Add(BitConverter.UInt64BitsToDouble(0x8000000000000000UL));

        Assert.Equal("2BC5822166BF4786", positiveZero.ToHexString());
        Assert.Equal("2BC6022166C02106", negativeZero.ToHexString());
        Assert.NotEqual(positiveZero.Value, negativeZero.Value);
    }

    [Fact]
    public void RepeatingSameValuesProducesSameHash()
    {
        var first = new StableHasher64();
        var second = new StableHasher64();

        first.Add("stable");
        first.Add(-42L);
        second.Add("stable");
        second.Add(-42L);

        Assert.Equal(first.Value, second.Value);
        Assert.Equal(first.ToHexString(), second.ToHexString());
    }

    [Fact]
    public void OrderChangesHash()
    {
        var first = new StableHasher64();
        var second = new StableHasher64();

        first.Add(1);
        first.Add(2);
        second.Add(2);
        second.Add(1);

        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void StringHashingRejectsNull()
    {
        var hasher = new StableHasher64();

        Assert.Throws<ArgumentNullException>(() => hasher.Add((string)null!));
    }

    [Fact]
    public void StringHashingRejectsMalformedUtf16InsteadOfReplacingIt()
    {
        const string UnpairedLowSurrogate = "\uDC00";
        var hasher = new StableHasher64();

        Assert.Throws<System.Text.EncoderFallbackException>(() => hasher.Add(UnpairedLowSurrogate));
    }
}
