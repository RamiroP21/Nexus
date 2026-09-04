namespace Nexus.Core.Tests;

public sealed class DeterministicSeedDerivationTests
{
    private static readonly DeterministicSeed Root = new(123_456_789);

    [Theory]
    [InlineData("", 0xF442E1569066163AUL, 0x3FFECB304C11E8BBUL)]
    [InlineData("Weather", 0x5516C74F34956E2BUL, 0x0929F6481D2987F0UL)]
    [InlineData("Economy", 0xAF44EBA77F1B484FUL, 0x107003E2B1456124UL)]
    [InlineData("District:7", 0x1EB2C6A4AA97B9D5UL, 0x28CDD74D86315B34UL)]
    public void DerivationMatchesFrozenFnvVectors(string key, ulong expectedSeed, ulong expectedStream)
    {
        Assert.Equal(new DeterministicSeed(expectedSeed), DeterministicSeedDerivation.Derive(Root, key));
        Assert.Equal(expectedStream, DeterministicSeedDerivation.DeriveStream(Root, key));
    }

    [Fact]
    public void SameInputAlwaysDerivesSameSeedAndStream()
    {
        DeterministicSeed firstSeed = DeterministicSeedDerivation.Derive(Root, "AI");
        ulong firstStream = DeterministicSeedDerivation.DeriveStream(Root, "AI");

        Assert.Equal(firstSeed, DeterministicSeedDerivation.Derive(Root, "AI"));
        Assert.Equal(firstStream, DeterministicSeedDerivation.DeriveStream(Root, "AI"));
    }

    [Fact]
    public void DifferentKeysDeriveDifferentSeedsStreamsAndSequences()
    {
        DeterministicSeed trafficSeed = DeterministicSeedDerivation.Derive(Root, "Traffic");
        DeterministicSeed weatherSeed = DeterministicSeedDerivation.Derive(Root, "Weather");
        ulong trafficStream = DeterministicSeedDerivation.DeriveStream(Root, "Traffic");
        ulong weatherStream = DeterministicSeedDerivation.DeriveStream(Root, "Weather");
        DeterministicRandom traffic = DeterministicSeedDerivation.CreateRandom(Root, "Traffic");
        DeterministicRandom weather = DeterministicSeedDerivation.CreateRandom(Root, "Weather");

        Assert.NotEqual(trafficSeed, weatherSeed);
        Assert.NotEqual(trafficStream, weatherStream);
        Assert.NotEqual(traffic.NextUInt64(), weather.NextUInt64());
    }

    [Fact]
    public void StreamsAreIndependentOfConsumptionInOtherStreams()
    {
        DeterministicRandom weatherBaseline = DeterministicRandom.ForStream(Root, "Weather");
        DeterministicRandom weatherAfterTraffic = DeterministicRandom.ForStream(Root, "Weather");
        DeterministicRandom traffic = DeterministicRandom.ForStream(Root, "Traffic");

        ulong[] expectedWeather = Enumerable.Range(0, 128)
            .Select(_ => weatherBaseline.NextUInt64())
            .ToArray();

        for (int index = 0; index < 10_000; index++)
        {
            _ = traffic.NextUInt32();
        }

        ulong[] actualWeather = Enumerable.Range(0, 128)
            .Select(_ => weatherAfterTraffic.NextUInt64())
            .ToArray();

        Assert.Equal(expectedWeather, actualWeather);
    }

    [Fact]
    public void KeyEncodingIsCaseSensitiveAndUsesUtf8()
    {
        DeterministicSeed lower = DeterministicSeedDerivation.Derive(Root, "weather");
        DeterministicSeed upper = DeterministicSeedDerivation.Derive(Root, "Weather");
        DeterministicSeed unicode = DeterministicSeedDerivation.Derive(Root, "ñ☃");

        Assert.NotEqual(lower, upper);
        Assert.Equal(new DeterministicSeed(0x4B72D688D7E2E24AUL), unicode);
    }

    [Fact]
    public void DerivationRejectsNullKey()
    {
        Assert.Throws<ArgumentNullException>(() => DeterministicSeedDerivation.Derive(Root, null!));
        Assert.Throws<ArgumentNullException>(() => DeterministicSeedDerivation.DeriveStream(Root, null!));
        Assert.Throws<ArgumentNullException>(() => DeterministicSeedDerivation.CreateRandom(Root, null!));
        Assert.Throws<ArgumentNullException>(() => DeterministicRandom.ForStream(Root, null!));
    }

    [Fact]
    public void DerivationRejectsMalformedUtf16InsteadOfReplacingIt()
    {
        const string UnpairedHighSurrogate = "\uD800";

        Assert.Throws<System.Text.EncoderFallbackException>(
            () => DeterministicSeedDerivation.Derive(Root, UnpairedHighSurrogate));
        Assert.Throws<System.Text.EncoderFallbackException>(
            () => DeterministicSeedDerivation.DeriveStream(Root, UnpairedHighSurrogate));
    }
}
