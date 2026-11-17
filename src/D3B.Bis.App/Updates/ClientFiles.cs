using System.Diagnostics;
using D3B.Bis.Data;
using D3B.Bis.Data.Packs;
using D3B.Bis.Data.Releases;
using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.App.Updates;

/// <summary>The installed client's files taken from its build's release, once Ankama updated the client (D440).</summary>
/// <remarks>
/// <para>
/// The host does not start without the build's pack, schema and mapping (D172). The window then offers to download them, each
/// checked against its SHA-256, into the directory the host looks in first, and to restart, as the host reads its files once
/// as it starts. The window reads where the download stands (<see cref="Status"/>) every refresh, off the thread that downloads.
/// </para>
/// <para>
/// A download that fails is said, never thrown, and may be tried again.
/// </para>
/// </remarks>
internal sealed class ClientFiles
{
    private readonly ClientReleaseDownloader _downloader;
    private readonly Lock _lock = new();
    private Task? _downloading;
    private volatile string? _failureMessage;
    private volatile bool _restart;
    private int _failure = -1;
    private int _progress;
    private int _status;

    /// <summary>Gets the names of the files the host reads, in the formats this code reads: the release's assets.</summary>
    public static IReadOnlyList<string> Names { get; } = [PackLocations.FileName, SchemaFile.FileName, MappingFile.FileName];

    /// <summary>Gets the repository whose releases ship the files, as owner/name.</summary>
    public string Repository { get; }

    /// <summary>Gets where the download stands.</summary>
    public ClientFilesStatus Status => (ClientFilesStatus)Volatile.Read(ref _status);

    /// <summary>Gets the share of the files downloaded, from 0 to 100.</summary>
    public int Progress => Volatile.Read(ref _progress);

    /// <summary>Gets why the last download failed, once it failed for the release; <see langword="null"/> when the disk refused the files.</summary>
    public ClientReleaseFailure? Failure => Volatile.Read(ref _failure) is var failure and >= 0 ? (ClientReleaseFailure)failure : null;

    /// <summary>Gets what the last download failed on, once it failed.</summary>
    public string? FailureMessage => _failureMessage;

    /// <summary>Gets whether the user asked to restart the app, which then reads the files downloaded.</summary>
    public bool RestartRequested => _restart;

    /// <summary>Initializes a new instance of the <see cref="ClientFiles"/> class.</summary>
    /// <param name="downloader">What takes the files from the build's release.</param>
    /// <param name="repository">The repository it reads, as owner/name, which the window names.</param>
    public ClientFiles(ClientReleaseDownloader downloader, string repository)
    {
        _downloader = downloader;
        Repository = repository;
    }

    /// <summary>Starts the app again, as it was started, once this process let go of the host.</summary>
    /// <param name="args">The command-line arguments the app was started with.</param>
    public static void StartAgain(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        // The app's own executable, installed or built from its code: dotnet run starts it the same way.
        if (Environment.ProcessPath is not { } path)
            return;

        var start = new ProcessStartInfo(path) { UseShellExecute = false, WorkingDirectory = Environment.CurrentDirectory };

        foreach (var argument in args)
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start);
    }

    /// <summary>Downloads the files of a build, off the caller's thread, into the directory of that build under a root.</summary>
    /// <param name="build">The installed client's build.</param>
    /// <param name="root">The directory the host looks in first, one directory per build under it.</param>
    /// <returns>A task that completes once the download is over; the one running when one already does.</returns>
    public Task DownloadAsync(string build, string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(build);
        ArgumentException.ThrowIfNullOrEmpty(root);

        lock (_lock)
        {
            if (_downloading is { IsCompleted: false } || Status is ClientFilesStatus.Ready)
                return _downloading ?? Task.CompletedTask;

            _failureMessage = null;
            Volatile.Write(ref _failure, -1);
            Volatile.Write(ref _progress, 0);
            SetStatus(ClientFilesStatus.Downloading);
            _downloading = Task.Run(() => DownloadCoreAsync(build, root));

            return _downloading;
        }
    }

    /// <summary>Asks for the restart that reads the files downloaded, once the app's window is gone and its host stopped.</summary>
    /// <returns>Whether the files are in place.</returns>
    public bool RequestRestart()
    {
        if (Status is not ClientFilesStatus.Ready)
            return false;

        _restart = true;

        return true;
    }

    private async Task DownloadCoreAsync(string build, string root)
    {
        try
        {
            var client = new ClientBuild(build, null);
            var progress = new Reporter(value => Volatile.Write(ref _progress, value.Total > 0 ? (int)(value.Downloaded * 100 / value.Total) : 0));

            await _downloader.DownloadAsync(client, Names, Path.Combine(root, client.Version), progress, CancellationToken.None).ConfigureAwait(false);

            Volatile.Write(ref _progress, 100);
            SetStatus(ClientFilesStatus.Ready);
        }
        catch (ClientReleaseException exception)
        {
            Fail(exception.Failure, exception.Message);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // The disk's, or a build whose name cannot be a directory: nothing the release can mend.
            Fail(null, exception.Message);
        }
    }

    private void Fail(ClientReleaseFailure? failure, string message)
    {
        _failureMessage = message;
        Volatile.Write(ref _failure, failure is { } value ? (int)value : -1);
        SetStatus(ClientFilesStatus.Failed);
    }

    private void SetStatus(ClientFilesStatus status)
    {
        Volatile.Write(ref _status, (int)status);
    }

    /// <summary>Reports on the thread that downloads, unlike <see cref="Progress{T}"/>, which would post to a context the download has not.</summary>
    private sealed class Reporter : IProgress<ClientDownloadProgress>
    {
        private readonly Action<ClientDownloadProgress> _report;

        public Reporter(Action<ClientDownloadProgress> report)
        {
            _report = report;
        }

        public void Report(ClientDownloadProgress value)
        {
            _report(value);
        }
    }
}
