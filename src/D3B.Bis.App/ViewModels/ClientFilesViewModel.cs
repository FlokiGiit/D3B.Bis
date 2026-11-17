using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D3B.Bis.App.Localization;
using D3B.Bis.App.Updates;
using D3B.Bis.Extraction;

namespace D3B.Bis.App.ViewModels;

/// <summary>The banner of a client Ankama updated: its files prepared on this PC, then a restart (D440).</summary>
internal sealed partial class ClientFilesViewModel : ObservableObject
{
    private readonly Localizer _localizer;
    private readonly AppUpdates? _updates;
    private readonly Action _close;
    private string? _build;
    private string? _root;
    private string? _client;

    /// <summary>Gets whether the host did not start for lack of the installed build's files, which the app may prepare.</summary>
    [ObservableProperty]
    public partial bool IsShown { get; private set; }

    /// <summary>Gets the banner's title: the client's new version, or what stops the files from being made.</summary>
    [ObservableProperty]
    public partial string Title { get; private set; } = "";

    /// <summary>Gets what the banner says of the files: what Update does, its step and progress, or why it failed.</summary>
    [ObservableProperty]
    public partial string Text { get; private set; } = "";

    /// <summary>Gets the label of the button that prepares the files: Update, then Try again once it failed.</summary>
    [ObservableProperty]
    public partial string UpdateLabel { get; private set; } = "";

    /// <summary>Gets whether the files may be prepared: neither preparing, nor in place, nor stopped by what only the code mends.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    public partial bool CanUpdate { get; private set; }

    /// <summary>Gets whether the button that prepares the files shows: not while it prepares, nor once they are in place, nor once only the code mends them.</summary>
    [ObservableProperty]
    public partial bool ShowsUpdate { get; private set; }

    /// <summary>Gets whether the files are being prepared.</summary>
    [ObservableProperty]
    public partial bool IsPreparing { get; private set; }

    /// <summary>Gets the share of the files prepared, from 0 to 100.</summary>
    [ObservableProperty]
    public partial int Progress { get; private set; }

    /// <summary>Gets whether the files are in place, which a restart reads.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    public partial bool IsReady { get; private set; }

    /// <summary>Gets whether the mapping no longer holds: the game's protocol changed and D3B.Bis's code must follow it.</summary>
    [ObservableProperty]
    public partial bool IsProtocolChanged { get; private set; }

    /// <summary>Gets whether the banner says what stops the files from being made, which only a change of the code mends.</summary>
    [ObservableProperty]
    public partial bool IsBlocked { get; private set; }

    /// <summary>Gets the preparation of the files, or <see langword="null"/> for an app that prepares none, such as in tests.</summary>
    public ClientFiles? Files { get; }

    /// <summary>Initializes a new instance of the <see cref="ClientFilesViewModel"/> class.</summary>
    /// <param name="localizer">The app's texts.</param>
    /// <param name="files">The preparation of the files, or <see langword="null"/> for an app that prepares none.</param>
    /// <param name="updates">The app's updates, whose version downloaded the restart installs too, or <see langword="null"/>.</param>
    /// <param name="close">Closes the app, which then starts again.</param>
    public ClientFilesViewModel(Localizer localizer, ClientFiles? files, AppUpdates? updates, Action close)
    {
        _localizer = localizer;
        Files = files;
        _updates = updates;
        _close = close;
    }

    /// <summary>Reads where the host and the preparation stand again, in the language chosen.</summary>
    /// <param name="missingBuild">The installed build whose files the host lacked, or <see langword="null"/>.</param>
    /// <param name="root">Where the host looks first for a build's files, or <see langword="null"/>.</param>
    /// <param name="clientDirectory">The installed client's directory, or <see langword="null"/>.</param>
    public void Refresh(string? missingBuild, string? root, string? clientDirectory)
    {
        _build = missingBuild;
        _root = root;
        _client = clientDirectory;
        IsShown = Files is not null && missingBuild is not null && root is not null && clientDirectory is not null;

        if (!IsShown || Files is null || missingBuild is null)
        {
            CanUpdate = ShowsUpdate = IsPreparing = IsReady = IsProtocolChanged = IsBlocked = false;

            return;
        }

        var status = Files.Status;
        var failure = status is ClientFilesStatus.Failed ? Files.Result?.Failure : null;

        IsPreparing = status is ClientFilesStatus.Preparing;
        IsReady = status is ClientFilesStatus.Ready;
        IsProtocolChanged = failure is PreparationFailure.ProtocolChanged;
        IsBlocked = failure is PreparationFailure.ProtocolChanged or PreparationFailure.ClientUnread;
        CanUpdate = status is ClientFilesStatus.Idle || (status is ClientFilesStatus.Failed && !IsBlocked);
        ShowsUpdate = !IsReady && !IsBlocked && !IsPreparing;
        Progress = Files.Progress;
        UpdateLabel = _localizer[status is ClientFilesStatus.Failed ? "ClientFilesRetry" : "ClientFilesUpdate"];
        Title = failure switch
        {
            PreparationFailure.ProtocolChanged => _localizer["ClientFilesProtocolTitle"],
            PreparationFailure.ClientUnread => _localizer["ClientFilesUnreadTitle"],
            _ => _localizer.Format("ClientFilesTitle", missingBuild),
        };
        Text = status switch
        {
            ClientFilesStatus.Preparing => _localizer.Format("ClientFilesPreparing", NumberOf(Files.Step), StepText(Files.Step), Progress),
            ClientFilesStatus.Ready => _localizer.Format("ClientFilesReady", missingBuild),
            ClientFilesStatus.Failed => FailureText(missingBuild, Files.Result),
            _ => _localizer["ClientFilesText"],
        };
    }

    private static int NumberOf(PreparationStep step)
    {
        return (int)step + 1;
    }

    private string StepText(PreparationStep step)
    {
        return step switch
        {
            PreparationStep.Pack => _localizer["ClientFilesStepPack"],
            PreparationStep.Schema => _localizer["ClientFilesStepSchema"],
            _ => _localizer["ClientFilesStepMapping"],
        };
    }

    private string FailureText(string build, PreparationResult? result)
    {
        var step = result?.Step ?? PreparationStep.Pack;
        var number = NumberOf(step);
        var name = StepText(step);
        var message = result?.Message ?? "";
        var log = Files?.LogPath ?? "";

        return result?.Failure switch
        {
            PreparationFailure.ProtocolChanged => _localizer.Format("ClientFilesProtocolChanged", build, log),
            PreparationFailure.ClientUnread => _localizer.Format("ClientFilesUnread", number, name, message, build, log),
            PreparationFailure.NoClient => _localizer.Format("ClientFilesNoClient", number, name, message),
            PreparationFailure.Disk => _localizer.Format("ClientFilesDisk", number, name, message),
            _ => _localizer.Format("ClientFilesUnexpected", number, name, message, log),
        };
    }

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private void Update()
    {
        if (Files is null || _build is null || _root is null || _client is null)
            return;

        _ = Files.PrepareAsync(_build, _client, _root);
        Refresh(_build, _root, _client);
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
