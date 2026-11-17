using System.Security.Cryptography;
using System.Text;
using D3B.Bis.App.Hosting;
using D3B.Bis.App.Localization;
using D3B.Bis.App.Tests.Updates;
using D3B.Bis.App.Updates;
using D3B.Bis.App.ViewModels;
using D3B.Bis.Bot.Hosting;
using D3B.Bis.Data.Releases;
using D3B.Bis.Data.Tests.Releases;

namespace D3B.Bis.App.Tests.ViewModels;

[UiThread]
public sealed class ClientFilesViewModelTests : IDisposable
{
    private const string Version = "3.7.14.18";
    private const string Tag = "client-3.7.14.18";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly Localizer _localizer = new(Localizer.English);
    private readonly FakeReleaseServer _server = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "D3B.Bis.App.Tests", Guid.NewGuid().ToString("N"));
    private int _closed;

    private static Dictionary<string, byte[]> Published => ClientFiles.Names.ToDictionary(name => name, name => Encoding.ASCII.GetBytes($"the bytes of {name}"), StringComparer.Ordinal);

    private string Target => Path.Combine(_root, Version);

    [Fact]
    public async Task AClientWhoseFilesAreMissingShowsItsVersionAndAnUpdateInsteadOfTheHostsFailure()
    {
        var main = await FailedAsync();

        Assert.True(main.ClientFiles.IsShown);
        Assert.Equal("The Dofus client moved to 3.7.14.18", main.ClientFiles.Title);
        Assert.Equal("D3B.Bis lacks this client's files. Update takes them from the release client-3.7.14.18 of Owner/Repo.", main.ClientFiles.Text);
        Assert.True(main.ClientFiles.DownloadCommand.CanExecute(null));
        Assert.Equal("Update", main.ClientFiles.DownloadLabel);
        Assert.False(main.ClientFiles.IsReady);
        Assert.False(main.ShowsHostFailure);
    }

    [Fact]
    public async Task UpdatingPutsTheReleasesFilesWhereTheHostLooksAndOffersARestart()
    {
        _server.Publish(Tag, Published);

        var main = await FailedAsync();

        await DownloadAsync(main);

        foreach (var (name, bytes) in Published)
            Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(Target, name), TestContext.Current.CancellationToken));

        Assert.True(main.ClientFiles.IsReady);
        Assert.Equal(100, main.ClientFiles.Progress);
        Assert.Equal("The files of client 3.7.14.18 are in place. Restart D3B.Bis to use them.", main.ClientFiles.Text);
        Assert.False(main.ClientFiles.DownloadCommand.CanExecute(null));

        main.ClientFiles.RestartCommand.Execute(null);

        Assert.Equal(1, _closed);
        Assert.True(main.ClientFiles.Files!.RestartRequested);
    }

    [Fact]
    public async Task ARestartWithTheAppsUpdateDownloadedInstallsItToo()
    {
        _server.Publish(Tag, Published);

        var updates = new AppUpdates(new FakeUpdateClient { Published = "0.2.0" });

        await updates.CheckAsync();

        var main = await FailedAsync(updates);

        await DownloadAsync(main);
        main.ClientFiles.RestartCommand.Execute(null);

        Assert.Equal(1, _closed);
        Assert.True(updates.RestartRequested);
    }

    [Fact]
    public async Task AReleaseNotPublishedYetIsSaidWithTheDesktopsLauncher()
    {
        var main = await FailedAsync();

        await DownloadAsync(main);

        Assert.Equal("The release client-3.7.14.18 of Owner/Repo is not published yet. Run \"D3B.Bis - Lancer.bat\" from the Desktop: it prepares the files on this PC.", main.ClientFiles.Text);
        Assert.False(main.ClientFiles.IsReady);
        Assert.True(main.ClientFiles.DownloadCommand.CanExecute(null));
        Assert.Equal("Try again", main.ClientFiles.DownloadLabel);
        Assert.False(Directory.Exists(Target));
    }

    [Fact]
    public async Task AReleaseWithoutTheFormatsReadIsSaidWithTheDesktopsLauncher()
    {
        var files = Published;

        files.Remove(ClientFiles.Names[0]);
        _server.Publish(Tag, files);

        var main = await FailedAsync();

        await DownloadAsync(main);

        Assert.Equal(
            $"The release client-3.7.14.18 of Owner/Repo does not hold the files this version reads ({string.Join(", ", ClientFiles.Names)}). Run \"D3B.Bis - Lancer.bat\" from the Desktop: it prepares the files on this PC.",
            main.ClientFiles.Text);
        Assert.False(Directory.Exists(Target));
    }

    [Fact]
    public async Task AFileThatDoesNotHoldItsChecksumIsSaidAndNothingIsPutInPlace()
    {
        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (name, bytes) in Published)
        {
            assets[name] = name == ClientFiles.Names[^1] ? [.. bytes, (byte)'!'] : bytes;
            assets[ClientRelease.ChecksumNameOf(name)] = Encoding.ASCII.GetBytes(ClientRelease.FormatChecksum(SHA256.HashData(bytes), name));
        }

        _server.PublishAssets(Tag, assets);

        var main = await FailedAsync();

        await DownloadAsync(main);

        Assert.Equal("A file downloaded does not match its checksum: nothing was put in place. Try again; if it happens again, run \"D3B.Bis - Lancer.bat\" from the Desktop.", main.ClientFiles.Text);
        Assert.True(main.ClientFiles.DownloadCommand.CanExecute(null));
        Assert.Empty(Directory.Exists(Target) ? Directory.GetFiles(Target) : []);
    }

    [Fact]
    public async Task ANetworkCutIsSaidAndTheUpdateMayBeTriedAgain()
    {
        _server.Publish(Tag, Published);
        _server.Cut(ClientFiles.Names[0], 4);

        var main = await FailedAsync();

        await DownloadAsync(main);

        Assert.StartsWith("The download failed: ", main.ClientFiles.Text, StringComparison.Ordinal);
        Assert.EndsWith("Nothing was put in place. Check the connection and try again.", main.ClientFiles.Text, StringComparison.Ordinal);
        Assert.True(main.ClientFiles.DownloadCommand.CanExecute(null));
        Assert.Empty(Directory.Exists(Target) ? Directory.GetFiles(Target) : []);

        _server.Cut(ClientFiles.Names[0], long.MaxValue);
        await DownloadAsync(main);

        Assert.True(main.ClientFiles.IsReady);
    }

    [Fact]
    public async Task TheBannerSpeaksTheAppsLanguage()
    {
        var main = await FailedAsync();

        _localizer.Use(Localizer.French);
        main.Refresh();

        Assert.Equal("Le client Dofus est passé en 3.7.14.18", main.ClientFiles.Title);
        Assert.Equal("Mettre à jour", main.ClientFiles.DownloadLabel);
    }

    [Fact]
    public async Task AHostThatFailedForAnotherReasonShowsItsFailureAndNoBanner()
    {
        var runner = new HostRunner(_ => Task.FromResult(new BotHostStart(null, "The mapping maps client 3.7.11.1.")));
        var main = new MainViewModel(runner, _localizer, null, null, () => _closed++, FromServer());

        runner.Start();
        await Waiting.UntilAsync(() => runner.Status is HostStatus.Failed, _timeout);
        main.Refresh();

        Assert.False(main.ClientFiles.IsShown);
        Assert.True(main.ShowsHostFailure);
        Assert.Equal("The mapping maps client 3.7.11.1.", main.HostFailure);
    }

    public void Dispose()
    {
        _server.Dispose();

        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private ClientFiles FromServer()
    {
        return new ClientFiles(new ClientReleaseDownloader(new HttpClient(_server, false), _server.Repository, null, FakeReleaseServer.Api), _server.Repository);
    }

    private async Task<MainViewModel> FailedAsync(AppUpdates? updates = null)
    {
        var runner = new HostRunner(_ => Task.FromResult(new BotHostStart(null, $"No pack for client {Version}.", Version, _root)));
        var main = new MainViewModel(runner, _localizer, null, updates, () => _closed++, FromServer());

        runner.Start();
        await Waiting.UntilAsync(() => runner.Status is HostStatus.Failed, _timeout);
        main.Refresh();

        return main;
    }

    private static async Task DownloadAsync(MainViewModel main)
    {
        main.ClientFiles.DownloadCommand.Execute(null);
        await Waiting.UntilAsync(() => main.ClientFiles.Files!.Status is not ClientFilesStatus.Downloading, _timeout);
        main.Refresh();
    }
}
