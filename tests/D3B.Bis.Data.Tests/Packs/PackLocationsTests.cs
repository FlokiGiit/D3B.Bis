using D3B.Bis.Data.Packs;

namespace D3B.Bis.Data.Tests.Packs;

public sealed class PackLocationsTests
{
    [Fact]
    public void APackIsKeptUnderItsClientVersionAndNamedAfterItsFormat()
    {
        var path = PackLocations.PathOf("root", new ClientBuild("3.7.12.12", null));

        Assert.Equal(Path.Combine("root", "3.7.12.12", $"data-{GamePack.FormatVersion}.pack"), path);
    }
}
