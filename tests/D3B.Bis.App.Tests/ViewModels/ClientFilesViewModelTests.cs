using D3B.Bis.App.Hosting;
using D3B.Bis.App.Localization;
using D3B.Bis.App.Tests.Updates;
using D3B.Bis.App.Updates;
using D3B.Bis.App.ViewModels;
using D3B.Bis.Bot.Hosting;
using D3B.Bis.Data.Packs;
using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.App.Tests.ViewModels;

[UiThread]
public sealed class ClientFilesViewModelTests : IDisposable
{
    private const string Version = "3.7.14.18";
    private const string Client = @"C:\Ankama\Dofus-dofus3";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    private readonly Localizer _localizer = new(Localizer.English);
    private readonly string _root = Path.Combine(Path.GetTempPath(), "D3B.Bis.App.Tests", Guid.NewGuid().ToString("N"));
    private readonly FakePreparationScript _script = new();
    private int _closed;

    private string Log => Path.Combine(_root, Version, ClientFiles.LogName);

    [Fact]
    public async Task AClientWhoseFilesAreMissingShowsItsVersionAndAnUpdateInsteadOfTheHostsFailure()
    {
        var main = await FailedAsync();

        Assert.True(main.ClientFiles.IsShown);
        Assert.Equal("The Dofus client moved to 3.7.14.18", main.ClientFiles.Title);
        Assert.Equal("D3B.Bis lacks this client's files. Update prepares them on this PC from the installed client, in about a minute.", main.ClientFiles.Text);
        Assert.True(main.ClientFiles.UpdateCommand.CanExecute(null));
        Assert.Equal("Update", main.ClientFiles.UpdateLabel);
        Assert.False(main.ClientFiles.IsReady);
        Assert.False(main.ShowsHostFailure);
    }

    [Fact]
    public async Task UpdatingPreparesThePackTheSchemaAndTheMappingFromTheClientWhereTheHostLooksThenOffersARestart()
    {
        var main = await FailedAsync();

        await UpdateAsync(main);

        Assert.Equal([$"pack {Client}", $"schema {Client}", $"mapping {Version}"], _script.Calls);
        Assert.True(File.Exists(Path.Combine(_root, Version, MappingFile.FileName)));
        Assert.True(main.ClientFiles.IsReady);
        Assert.Equal(100, main.ClientFiles.Progress);
        Assert.Equal("The files of client 3.7.14.18 are ready. Restart D3B.Bis to use them.", main.ClientFiles.Text);
        Assert.False(main.ClientFiles.UpdateCommand.CanExecute(null));

        main.ClientFiles.RestartCommand.Execute(null);

        Assert.Equal(1, _closed);
        Assert.True(main.ClientFiles.Files!.RestartRequested);
    }

    [Fact]
    public async Task WhileItPreparesTheBannerSaysTheStepAndItsProgress()
    {
        using var gate = new SemaphoreSlim(0);
        var script = new FakePreparationScript { Gate = gate };
        var main = await FailedAsync(script: script);

        main.ClientFiles.UpdateCommand.Execute(null);
        await Waiting.UntilAsync(() => main.ClientFiles.Files!.Progress > 0, _timeout);
        main.Refresh();

        Assert.True(main.ClientFiles.IsPreparing);
        Assert.InRange(main.ClientFiles.Progress, 30, 50);
        Assert.Equal($"Step 1 of 3: reading the client's data… {main.ClientFiles.Progress} %", main.ClientFiles.Text);
        Assert.False(main.ClientFiles.UpdateCommand.CanExecute(null));
        Assert.False(main.ClientFiles.ShowsUpdate);
        Assert.False(main.ClientFiles.IsReady);

        gate.Release();
        await Waiting.UntilAsync(() => main.ClientFiles.Files!.Status is not ClientFilesStatus.Preparing, _timeout);
        main.Refresh();

        Assert.True(main.ClientFiles.IsReady);
    }

    [Fact]
    public async Task AMappingThatNoLongerHoldsSaysTheGamesProtocolChangedAndOffersNoRetry()
    {
        _script.MappingHolds = false;

        var main = await FailedAsync();

        await UpdateAsync(main);

        Assert.True(main.ClientFiles.IsProtocolChanged);
        Assert.Equal("The game's protocol changed", main.ClientFiles.Title);
        Assert.Equal(
            $"Client 3.7.14.18 speaks a protocol D3B.Bis no longer maps: its code must follow it before a bot plays on this client. Nothing was put in place; what did not hold is written in {Log}.",
            main.ClientFiles.Text);
        Assert.False(File.Exists(Path.Combine(_root, Version, PackLocations.FileName)));
        Assert.False(main.ClientFiles.UpdateCommand.CanExecute(null));
        Assert.False(main.ClientFiles.IsReady);
    }

    [Fact]
    public async Task WhatTheStepsSayIsKeptBesideTheBuildsFiles()
    {
        _script.MappingHolds = false;

        var main = await FailedAsync();

        await UpdateAsync(main);

        var lines = await File.ReadAllLinesAsync(Log, TestContext.Current.CancellationToken);

        Assert.Contains("Pack: read from the fake client", lines);
        Assert.Contains("  failure: d3b.game.MapChangeEvent has no counterpart", lines);
    }

    [Fact]
    public async Task AClientTheExtractionMisreadsIsSaidWithItsStepAndOffersNoRetry()
    {
        _script.SchemaThrows = new InvalidDataException("The client has no assembly Ankama.Dofus.Protocol.Game.");

        var main = await FailedAsync();

        await UpdateAsync(main);

        Assert.Equal("D3B.Bis cannot read this client", main.ClientFiles.Title);
        Assert.Equal(
            $"Step 2 of 3, reading the client's protocol, failed: The client has no assembly Ankama.Dofus.Protocol.Game. Client 3.7.14.18 changed in a way D3B.Bis's code must follow; the details are in {Log}.",
            main.ClientFiles.Text);
        Assert.False(main.ClientFiles.UpdateCommand.CanExecute(null));
    }

    [Fact]
    public async Task AFileTheDiskRefusesIsSaidAndTheUpdateMayBeTriedAgain()
    {
        _script.PackThrows = new IOException("There is not enough space on the disk.");

        var main = await FailedAsync();

        await UpdateAsync(main);

        Assert.Equal("Step 1 of 3, reading the client's data, failed: There is not enough space on the disk. Try again once the cause is fixed.", main.ClientFiles.Text);
        Assert.True(main.ClientFiles.UpdateCommand.CanExecute(null));
        Assert.Equal("Try again", main.ClientFiles.UpdateLabel);

        _script.PackThrows = null;
        await UpdateAsync(main);

        Assert.True(main.ClientFiles.IsReady);
    }

    [Fact]
    public async Task ARestartWithTheAppsUpdateDownloadedInstallsItToo()
    {
        var updates = new AppUpdates(new FakeUpdateClient { Published = "0.2.0" });

        await updates.CheckAsync();

        var main = await FailedAsync(updates);

        await UpdateAsync(main);
        main.ClientFiles.RestartCommand.Execute(null);

        Assert.Equal(1, _closed);
        Assert.True(updates.RestartRequested);
    }

    [Fact]
    public async Task TheBannerSpeaksTheAppsLanguage()
    {
        var main = await FailedAsync();

        _localizer.Use(Localizer.French);
        main.Refresh();

        Assert.Equal("Le client Dofus est passé en 3.7.14.18", main.ClientFiles.Title);
        Assert.Equal("Mettre à jour", main.ClientFiles.UpdateLabel);
    }

    [Fact]
    public async Task AHostThatFailedForAnotherReasonShowsItsFailureAndNoBanner()
    {
        var runner = new HostRunner(_ => Task.FromResult(new BotHostStart(null, "The mapping maps client 3.7.11.1.")));
        var main = new MainViewModel(runner, _localizer, null, null, () => _closed++, Files(_script));

        runner.Start();
        await Waiting.UntilAsync(() => runner.Status is HostStatus.Failed, _timeout);
        main.Refresh();

        Assert.False(main.ClientFiles.IsShown);
        Assert.True(main.ShowsHostFailure);
        Assert.Equal("The mapping maps client 3.7.11.1.", main.HostFailure);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private static ClientFiles Files(FakePreparationScript script)
    {
        return new ClientFiles(log => new FakePreparation(log, script));
    }

    private static async Task UpdateAsync(MainViewModel main)
    {
        main.ClientFiles.UpdateCommand.Execute(null);
        await Waiting.UntilAsync(() => main.ClientFiles.Files!.Status is not ClientFilesStatus.Preparing, _timeout);
        main.Refresh();
    }

    private async Task<MainViewModel> FailedAsync(AppUpdates? updates = null, FakePreparationScript? script = null)
    {
        var runner = new HostRunner(_ => Task.FromResult(new BotHostStart(null, $"No pack for client {Version}.", Version, _root, Client)));
        var main = new MainViewModel(runner, _localizer, null, updates, () => _closed++, Files(script ?? _script));

        runner.Start();
        await Waiting.UntilAsync(() => runner.Status is HostStatus.Failed, _timeout);
        main.Refresh();

        return main;
    }
}
