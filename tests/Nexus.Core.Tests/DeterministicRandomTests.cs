namespace Nexus.Core.Tests;

public sealed class DeterministicRandomTests
{
    private static readonly uint[] ReferencePcg32Sequence =
    [
        0xA15C02B7U,
        0x7B47F409U,
        0xBA1D3330U,
        0x83D2F293U,
        0xBFA4784BU,
        0xCBED606EU,
        0xBFC6A3ADU,
        0x812FFF6DU,
        0xE61F305AU,
        0xF9384B90U,
    ];

    [Fact]
    public void NextUInt32MatchesFrozenReferencePcgVector()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42), stream: 54);

        uint[] actual = Enumerable.Range(0, ReferencePcg32Sequence.Length)
            .Select(_ => random.NextUInt32())
            .ToArray();

        Assert.Equal(ReferencePcg32Sequence, actual);
    }

    [Fact]
    public void DefaultConstructorUsesDocumentedReferenceStream()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42));

        Assert.Equal(54UL, random.Stream);
        Assert.Equal(ReferencePcg32Sequence[0], random.NextUInt32());
    }

    [Fact]
    public void NextUInt64ComposesFirstDrawAsHighBits()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42), stream: 54);

        ulong[] expected =
        [
            0xA15C02B77B47F409UL,
            0xBA1D333083D2F293UL,
            0xBFA4784BCBED606EUL,
            0xBFC6A3AD812FFF6DUL,
        ];

        ulong[] actual = Enumerable.Range(0, expected.Length)
            .Select(_ => random.NextUInt64())
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NextDoubleHasFrozenIeeeRepresentations()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42), stream: 54);
        ulong[] expectedBits =
        [
            0x3FE42B8056EF68FEUL,
            0x3FE743A666107A5EUL,
            0x3FE7F48F09797DACUL,
            0x3FE7F8D475B025FFUL,
        ];

        ulong[] actualBits = Enumerable.Range(0, expectedBits.Length)
            .Select(_ => BitConverter.DoubleToUInt64Bits(random.NextDouble()))
            .ToArray();

        Assert.Equal(expectedBits, actualBits);
    }

    [Fact]
    public void NextDoubleAlwaysReturnsUnitIntervalValues()
    {
        var random = new DeterministicRandom(new DeterministicSeed(987_654_321));

        for (int index = 0; index < 50_000; index++)
        {
            double value = random.NextDouble();
            Assert.True(value >= 0.0);
            Assert.True(value < 1.0);
        }
    }

    [Fact]
    public void NextIntMatchesFrozenSequence()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42), stream: 54);
        int[] expected = [6, 0, 7, 18, 18, -11, -12, -12, 17, 7, 5, 16];

        int[] actual = Enumerable.Range(0, expected.Length)
            .Select(_ => random.NextInt(-17, 23))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 10)]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(int.MinValue, int.MinValue + 1)]
    [InlineData(int.MaxValue - 1, int.MaxValue)]
    public void NextIntAlwaysHonorsHalfOpenRange(int minimum, int maximum)
    {
        var random = new DeterministicRandom(new DeterministicSeed(18_446), stream: 7);

        for (int index = 0; index < 20_000; index++)
        {
            int value = random.NextInt(minimum, maximum);
            Assert.InRange(value, minimum, maximum - 1);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void NextIntRejectsEmptyOrReversedRanges(int minimum, int maximum)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DeterministicRandom(new DeterministicSeed(1)).NextInt(minimum, maximum));

        Assert.Equal("maxExclusive", exception.ParamName);
    }

    [Fact]
    public void StreamSelectorUsesOnlyThePcgDefined63Bits()
    {
        const ulong lowBits = 123_456UL;
        var low = new DeterministicRandom(new DeterministicSeed(99), lowBits);
        var high = new DeterministicRandom(new DeterministicSeed(99), lowBits | (1UL << 63));

        Assert.Equal(lowBits, low.Stream);
        Assert.Equal(low.Stream, high.Stream);
        Assert.Equal(low.NextUInt32(), high.NextUInt32());
    }

    [Fact]
    public void ReproducibleStateCanParticipateInStableHash()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42), stream: 54);
        var hasher = new StableHasher64();

        random.ContributeToHash(hasher);

        Assert.Equal(0x185706B82C2E03F8UL, random.State);
        Assert.Equal("79F41D037DCA9EAD", hasher.ToHexString());
    }

    [Fact]
    public void ContributeToHashRejectsNullHasher()
    {
        var random = new DeterministicRandom(new DeterministicSeed(42));

        Assert.Throws<ArgumentNullException>(() => random.ContributeToHash(null!));
    }
}
