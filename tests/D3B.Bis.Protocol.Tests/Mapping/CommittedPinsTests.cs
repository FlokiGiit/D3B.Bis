using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.Protocol.Tests.Mapping;

public sealed class CommittedPinsTests
{
    private static string[] Files => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Pins"), "*.json");

    [Fact]
    public void TheProtocolCarriesEveryCommittedBuildsPinsOldestFirst()
    {
        var committed = Files.Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(committed, CommittedPins.Builds.Order(StringComparer.Ordinal));
        Assert.All(CommittedPins.Builds.Zip(CommittedPins.Builds.Skip(1)), pair => Assert.True(Version.Parse(pair.First) < Version.Parse(pair.Second), $"{pair.First} before {pair.Second}"));
    }

    [Fact]
    public void ACommittedBuildReadsItsOwnPins()
    {
        foreach (var file in Files)
        {
            var build = Path.GetFileNameWithoutExtension(file);

            Assert.Equal(build, CommittedPins.SourceOf(build));
            Assert.Equal(PinnedPairsFile.Read(file, build), CommittedPins.For(build));
        }
    }

    [Fact]
    public void ABuildWithoutPinsTakesTheLatestBuildsCarriedOverToIt()
    {
        var latest = CommittedPins.Builds[^1];
        var expected = PinnedPairsFile.Read(PinnedPairsFile.PathOf(Path.Combine(AppContext.BaseDirectory, "Pins"), latest), latest).Clone();

        expected.ClientVersion = "99.0.0.1";

        Assert.Equal(latest, CommittedPins.SourceOf("99.0.0.1"));
        Assert.Equal(expected, CommittedPins.For("99.0.0.1"));
        Assert.NotEmpty(CommittedPins.For("99.0.0.1").Messages);
    }

    [Fact]
    public void TheBuildsAreOrderedByTheirNumbersNotTheirText()
    {
        Assert.Equal(["3.7.9.1", "3.7.13.17", "3.10.0.2"], CommittedPins.Ordered(["3.7.13.17", "3.10.0.2", "3.7.9.1"]));
    }

    [Fact]
    public void ANameThatIsNoBuildIsRefused()
    {
        Assert.Throws<ArgumentException>(() => CommittedPins.For("../3.7.13.17"));
        Assert.Throws<ArgumentException>(() => CommittedPins.SourceOf(""));
    }
}
