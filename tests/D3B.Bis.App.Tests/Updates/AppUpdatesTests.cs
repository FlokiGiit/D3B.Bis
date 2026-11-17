using D3B.Bis.App.Updates;
using Microsoft.Extensions.Configuration;

namespace D3B.Bis.App.Tests.Updates;

public sealed class AppUpdatesTests
{
    [Fact]
    public async Task AnAppRunFromItsCodeNeverChecks()
    {
        var client = new FakeUpdateClient { IsInstalled = false, Published = "0.2.0" };
        var updates = new AppUpdates(client);

        await updates.CheckAsync();

        Assert.Equal(UpdateStatus.NotInstalled, updates.Status);
        Assert.Equal(0, client.Checks);
    }

    [Fact]
    public async Task ANewerVersionIsDownloadedAndInstalledOnceTheAppCloses()
    {
        var client = new FakeUpdateClient { Published = "0.2.0" };
        var updates = new AppUpdates(client);

        await updates.CheckAsync();

        Assert.Equal(UpdateStatus.Ready, updates.Status);
        Assert.Equal("0.2.0", updates.NewVersion);
        Assert.Equal(100, updates.Progress);
        Assert.True(client.Downloaded);

        updates.Finish();

        Assert.True(client.AppliedOnExit);
        Assert.False(client.AppliedAndRestarted);
    }

    [Fact]
    public async Task ARestartAskedForInstallsTheVersionAndStartsTheAppAgain()
    {
        var client = new FakeUpdateClient { Published = "0.2.0" };
        var updates = new AppUpdates(client);

        await updates.CheckAsync();

        Assert.True(updates.RequestRestart());

        updates.Finish();

        Assert.True(client.AppliedAndRestarted);
        Assert.False(client.AppliedOnExit);
    }

    [Fact]
    public async Task WithoutANewerVersionNothingIsInstalled()
    {
        var client = new FakeUpdateClient();
        var updates = new AppUpdates(client);

        await updates.CheckAsync();

        Assert.Equal(UpdateStatus.UpToDate, updates.Status);
        Assert.False(updates.RequestRestart());

        updates.Finish();

        Assert.False(client.Downloaded);
        Assert.False(client.AppliedOnExit);
        Assert.False(client.AppliedAndRestarted);
    }

    [Fact]
    public async Task AFailedCheckIsSaidAndCheckedAgainOnDemand()
    {
        var client = new FakeUpdateClient { CheckFails = new HttpRequestException("No such host is known.") };
        var updates = new AppUpdates(client);

        await updates.CheckAsync();

        Assert.Equal(UpdateStatus.Failed, updates.Status);
        Assert.Equal("No such host is known.", updates.Failure);

        client.CheckFails = null;
        client.Published = "0.2.0";
        await updates.CheckAsync();

        Assert.Equal(UpdateStatus.Ready, updates.Status);
        Assert.Null(updates.Failure);
        Assert.Equal(2, client.Checks);
    }

    [Fact]
    public async Task AVersionDownloadedIsNotCheckedAgain()
    {
        var client = new FakeUpdateClient { Published = "0.2.0" };
        var updates = new AppUpdates(client);

        await updates.CheckAsync();
        await updates.CheckAsync();

        Assert.Equal(1, client.Checks);
    }

    [Fact]
    public void TheSourceIsTheRepositoryUnlessTheConfigurationNamesOne()
    {
        var none = new ConfigurationBuilder().Build();
        var folder = new ConfigurationBuilder().AddCommandLine(["--Updates:Source", @"C:\releases"]).Build();

        Assert.Equal("https://github.com/FlokiGiit/D3B.Bis", UpdateSettings.SourceIn(none));
        Assert.Equal(@"C:\releases", UpdateSettings.SourceIn(folder));
    }

    [Fact]
    public void TheClientsFilesComeFromD3BsReleasesUnlessTheConfigurationNamesAnotherRepository()
    {
        var none = new ConfigurationBuilder().Build();
        var other = new ConfigurationBuilder().AddCommandLine(["--Updates:ClientRepository", "Someone/Fork"]).Build();
        var wrong = new ConfigurationBuilder().AddCommandLine(["--Updates:ClientRepository", "not a repository"]).Build();

        Assert.Equal("AerafalDev/D3B", UpdateSettings.ClientRepositoryIn(none));
        Assert.Equal("Someone/Fork", UpdateSettings.ClientRepositoryIn(other));
        Assert.Equal("AerafalDev/D3B", UpdateSettings.ClientRepositoryIn(wrong));
    }
}
