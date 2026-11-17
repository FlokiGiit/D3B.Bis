using System.Diagnostics;
using D3B.Bis.Extraction;

namespace D3B.Bis.App.Updates;

/// <summary>The installed client's files prepared on this PC once Ankama updated the client: its pack, schema and mapping (D440).</summary>
/// <remarks>
/// <para>
/// The host does not start without the build's pack, schema and mapping (D172). The window then offers to prepare them from
/// the installed client, in the app's own process, into the directory the host looks in first, and to restart, as the host
/// reads its files once as it starts. The window reads where the preparation stands (<see cref="Status"/>) every refresh, off
/// the thread that prepares.
/// </para>
/// <para>
/// What the steps say is kept in <see cref="LogName"/> beside the build's files, so that a mapping that no longer holds can be
/// looked at. A preparation that fails is said, never thrown.
/// </para>
/// </remarks>
internal sealed class ClientFiles
{
    /// <summary>The name of the file, in the build's directory, that holds what the steps said.</summary>
    public const string LogName = "preparation.log";

    private readonly Func<TextWriter, IPreparationSteps> _steps;
    private readonly Lock _lock = new();
    private Task? _preparing;
    private volatile PreparationResult? _result;
    private volatile string? _logPath;
    private volatile bool _restart;
    private int _progress;
    private int _step;
    private int _status;

    /// <summary>Gets where the preparation stands.</summary>
    public ClientFilesStatus Status => (ClientFilesStatus)Volatile.Read(ref _status);

    /// <summary>Gets the step running, or the last one run.</summary>
    public PreparationStep Step => (PreparationStep)Volatile.Read(ref _step);

    /// <summary>Gets the share of the preparation done, from 0 to 100.</summary>
    public int Progress => Volatile.Read(ref _progress);

    /// <summary>Gets what the last preparation gave, once it ended; <see langword="null"/> before.</summary>
    public PreparationResult? Result => _result;

    /// <summary>Gets the file that holds what the steps of the last preparation said, once one started.</summary>
    public string? LogPath => _logPath;

    /// <summary>Gets whether the user asked to restart the app, which then reads the files prepared.</summary>
    public bool RestartRequested => _restart;

    /// <summary>Initializes a new instance of the <see cref="ClientFiles"/> class.</summary>
    /// <param name="steps">Makes what each step does, given where the steps say what they did.</param>
    public ClientFiles(Func<TextWriter, IPreparationSteps> steps)
    {
        _steps = steps;
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

    /// <summary>Prepares the files of a build, off the caller's thread, into the directory of that build under a root.</summary>
    /// <param name="build">The installed client's build.</param>
    /// <param name="clientDirectory">The client's installation directory.</param>
    /// <param name="root">The directory the host looks in first, one directory per build under it.</param>
    /// <returns>A task that completes once the preparation is over; the one running when one already does.</returns>
    public Task PrepareAsync(string build, string clientDirectory, string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(build);
        ArgumentException.ThrowIfNullOrEmpty(clientDirectory);
        ArgumentException.ThrowIfNullOrEmpty(root);

        lock (_lock)
        {
            if (_preparing is { IsCompleted: false } || Status is ClientFilesStatus.Ready)
                return _preparing ?? Task.CompletedTask;

            _result = null;
            Volatile.Write(ref _progress, 0);
            Volatile.Write(ref _step, (int)PreparationStep.Pack);
            SetStatus(ClientFilesStatus.Preparing);
            _preparing = Task.Run(() => Prepare(build, clientDirectory, root));

            return _preparing;
        }
    }

    /// <summary>Asks for the restart that reads the files prepared, once the app's window is gone and its host stopped.</summary>
    /// <returns>Whether the files are in place.</returns>
    public bool RequestRestart()
    {
        if (Status is not ClientFilesStatus.Ready)
            return false;

        _restart = true;

        return true;
    }

    private void Prepare(string build, string clientDirectory, string root)
    {
        PreparationResult result;

        try
        {
            var directory = Path.Combine(root, build);

            Directory.CreateDirectory(directory);
            _logPath = Path.Combine(directory, LogName);

            // The steps may say a line from more than one thread, as the extraction reads several bundles at once.
            using var writer = new StreamWriter(_logPath, false) { AutoFlush = true };
            using var log = TextWriter.Synchronized(writer);

            result = new ClientPreparation(_steps(log)).Prepare(build, clientDirectory, root, new Reporter(this));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // The log's own directory or file, before any step ran: nothing the client's files have to do with.
            result = new PreparationResult(PreparationFailure.Disk, PreparationStep.Pack, exception.Message);
        }

        // The extraction peaks at 3.4 GB on 3.7.14.18 (D440): a window left open on a failure should not keep that heap.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        _result = result;

        if (result.IsReady)
            Volatile.Write(ref _progress, 100);

        SetStatus(result.IsReady ? ClientFilesStatus.Ready : ClientFilesStatus.Failed);
    }

    private void SetStatus(ClientFilesStatus status)
    {
        Volatile.Write(ref _status, (int)status);
    }

    /// <summary>Reports on the thread that prepares, unlike <see cref="Progress{T}"/>, which would post to a context the preparation has not.</summary>
    private sealed class Reporter : IProgress<PreparationProgress>
    {
        private readonly ClientFiles _files;

        public Reporter(ClientFiles files)
        {
            _files = files;
        }

        public void Report(PreparationProgress value)
        {
            Volatile.Write(ref _files._step, (int)value.Step);
            Volatile.Write(ref _files._progress, (int)(value.Done * 100));
        }
    }
}
