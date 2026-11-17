using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D3B.Bis.App.Localization;
using D3B.Bis.App.Updates;
using D3B.Bis.Data.Releases;

namespace D3B.Bis.App.ViewModels;

/// <summary>The banner of a client Ankama updated: its files taken from the build's release, then a restart (D440).</summary>
internal sealed partial class ClientFilesViewModel : ObservableObject
{
    private const string Launcher = "D3B.Bis - Lancer.bat";

    private readonly Localizer _localizer;
    private readonly AppUpdates? _updates;
    private readonly Action _close;
    private string? _build;
    private string? _root;

    /// <summary>Gets whether the host did not start for lack of the installed build's files, which the release may hold.</summary>
    [ObservableProperty]
    public partial bool IsShown { get; private set; }

    /// <summary>Gets the banner's title: the client's new version.</summary>
    [ObservableProperty]
    public partial string Title { get; private set; } = "";

    /// <summary>Gets what the banner says of the files: what Update does, its progress, or why it failed.</summary>
    [ObservableProperty]
    public partial string Text { get; private set; } = "";

    /// <summary>Gets the label of the button that downloads the files: Update, then Try again once it failed.</summary>
    [ObservableProperty]
    public partial string DownloadLabel { get; private set; } = "";

    /// <summary>Gets whether the files may be downloaded: neither downloading nor in place.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial bool CanDownload { get; private set; }

    /// <summary>Gets whether the files are being downloaded.</summary>
    [ObservableProperty]
    public partial bool IsDownloading { get; private set; }

    /// <summary>Gets the share of the files downloaded, from 0 to 100.</summary>
    [ObservableProperty]
    public partial int Progress { get; private set; }

    /// <summary>Gets whether the files are in place, which a restart reads.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    public partial bool IsReady { get; private set; }

    /// <summary>Gets whether the last download failed.</summary>
    [ObservableProperty]
    public partial bool IsFailed { get; private set; }

    /// <summary>Gets the download of the files, or <see langword="null"/> for an app that downloads none, such as in tests.</summary>
    public ClientFiles? Files { get; }

    /// <summary>Initializes a new instance of the <see cref="ClientFilesViewModel"/> class.</summary>
    /// <param name="localizer">The app's texts.</param>
    /// <param name="files">The download of the files, or <see langword="null"/> for an app that downloads none.</param>
    /// <param name="updates">The app's updates, whose version downloaded the restart installs too, or <see langword="null"/>.</param>
    /// <param name="close">Closes the app, which then starts again.</param>
    public ClientFilesViewModel(Localizer localizer, ClientFiles? files, AppUpdates? updates, Action close)
    {
        _localizer = localizer;
        Files = files;
        _updates = updates;
        _close = close;
    }

    /// <summary>Reads where the host and the download stand again, in the language chosen.</summary>
    /// <param name="missingBuild">The installed build whose files the host lacked, or <see langword="null"/>.</param>
    /// <param name="root">Where the host looks first for a build's files, or <see langword="null"/>.</param>
    public void Refresh(string? missingBuild, string? root)
    {
        _build = missingBuild;
        _root = root;
        IsShown = Files is not null && missingBuild is not null && root is not null;

        if (!IsShown || Files is null || missingBuild is null)
        {
            CanDownload = IsDownloading = IsReady = IsFailed = false;

            return;
        }

        var status = Files.Status;

        IsDownloading = status is ClientFilesStatus.Downloading;
        IsReady = status is ClientFilesStatus.Ready;
        IsFailed = status is ClientFilesStatus.Failed;
        CanDownload = status is ClientFilesStatus.Idle or ClientFilesStatus.Failed;
        Progress = Files.Progress;
        Title = _localizer.Format("ClientFilesTitle", missingBuild);
        DownloadLabel = _localizer[IsFailed ? "ClientFilesRetry" : "ClientFilesUpdate"];
        Text = status switch
        {
            ClientFilesStatus.Downloading => _localizer.Format("ClientFilesDownloading", missingBuild, Progress),
            ClientFilesStatus.Ready => _localizer.Format("ClientFilesReady", missingBuild),
            ClientFilesStatus.Failed => FailureText(missingBuild),
            _ => _localizer.Format("ClientFilesText", missingBuild, Files.Repository),
        };
    }

    private string FailureText(string build)
    {
        return Files?.Failure switch
        {
            ClientReleaseFailure.NotPublished => _localizer.Format("ClientFilesNotPublished", build, Files.Repository, Launcher),
            ClientReleaseFailure.MissingFiles => _localizer.Format("ClientFilesMissing", build, Files.Repository, string.Join(", ", ClientFiles.Names), Launcher),
            ClientReleaseFailure.Checksum => _localizer.Format("ClientFilesChecksum", Launcher),
            ClientReleaseFailure.Network => _localizer.Format("ClientFilesNetwork", Files.FailureMessage ?? ""),
            _ => _localizer.Format("ClientFilesWrite", Files?.FailureMessage ?? ""),
        };
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private void Download()
    {
        if (Files is null || _build is null || _root is null)
            return;

        _ = Files.DownloadAsync(_build, _root);
        Refresh(_build, _root);
    }

    [RelayCommand(CanExecute = nameof(IsReady))]
    private void Restart()
    {
        if (Files?.RequestRestart() is not true)
            return;

        // The app's own update, once downloaded, is installed by the same restart rather than left for the next exit (D173).
        _updates?.RequestRestart();
        _close();
    }
}
