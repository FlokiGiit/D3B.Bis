namespace D3B.Bis.Hooks.Tests;

public sealed class WriteGuardTests
{
    [Theory]
    [InlineData("data-11.pack")]
    [InlineData("src/D3B.Bis.Data/data-11.pack.sha256")]
    [InlineData("mapping-17.binpb")]
    [InlineData("schema-1.binpb")]
    [InlineData("tests/fixtures/session.d3bcap")]
    [InlineData("d3b.db")]
    [InlineData("src/D3B.Bis.Storage/d3b.db-wal")]
    [InlineData("assets/UI.bundle")]
    [InlineData("global-metadata.dat")]
    [InlineData("DATA-11.PACK")]
    public void WhatIsExtractedFromTheClientOrAMachineIsRefused(string path)
    {
        Assert.NotNull(WriteGuard.Refusal(path));
    }

    [Theory]
    [InlineData("vendor/BillBot/Program.cs")]
    [InlineData("vendor/x.md")]
    public void TheReferencesInVendorAreRefused(string path)
    {
        Assert.NotNull(WriteGuard.Refusal(path));
    }

    [Theory]
    [InlineData("scripts/out/game.proto")]
    [InlineData("docs/schema/game.proto")]
    [InlineData("game.proto")]
    public void ProtoTextOutsideTheProtocolProjectIsRefused(string path)
    {
        Assert.NotNull(WriteGuard.Refusal(path));
    }

    [Theory]
    [InlineData("src/D3B.Bis.Protocol/Game/fights.proto")]
    [InlineData("src/D3B.Bis.Protocol/Pins/3.7.13.17.json")]
    [InlineData("src/D3B.Bis.Engine/Fights/FightJudge.cs")]
    [InlineData("src/D3B.Bis.App/Assets/d3b.png")]
    [InlineData("src/D3B.Bis.Bot/Captures.cs")]
    [InlineData("src/D3B.Bis.Network/Captures/CaptureReader.cs")]
    [InlineData("tests/D3B.Bis.Network.Tests/Captures/CaptureTests.cs")]
    [InlineData("tests/D3B.Bis.Protocol.Tests/Fixtures/fixture.proto")]
    [InlineData("scripts/capture/CaptureCommand.cs")]
    [InlineData("docs/decisions/README.md")]
    [InlineData(".claude/hooks/guard-git.cs")]
    [InlineData("tests/D3B.Bis.Hooks.Tests/WriteGuardTests.cs")]
    public void OrdinaryProjectFilesPass(string path)
    {
        Assert.Null(WriteGuard.Refusal(path));
    }

    [Theory]
    [InlineData("artifacts/studies/fights.md")]
    [InlineData("artifacts/review/x.diff")]
    [InlineData("artifacts/captures/x.d3bcap")]
    public void TheIgnoredArtifactsFolderIsOpen(string path)
    {
        Assert.Null(WriteGuard.Refusal(path));
    }
}
