using Avalonia;
using D3B.Bis.App.Hosting;
using D3B.Bis.App.Updates;
using D3B.Bis.Extraction;
using D3B.Bis.Storage;
using Microsoft.Extensions.Configuration;
using Velopack;

namespace D3B.Bis.App;

/// <summary>Starts the bots' host, then the window; stops the host once the window is gone (D91).</summary>
internal static class Program
{
    /// <summary>Runs the app.</summary>
    /// <param name="args">The command-line arguments, read as the host's configuration, as <c>D3B.Bis.Host</c> reads them.</param>
    /// <returns>The app's exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        // First of all: run by its installer, an update or its uninstaller, the app does what they ask and ends here (D171).
        VelopackApp.Build().Run();

        // Before the preferences and the host are read: what D3B kept on this machine comes across at the first start.
        PreviousInstall.Import(PreviousInstall.PreviousRoot, PreviousInstall.Root);

        // Before the host: it starts with the settings the app changed (D151).
        var preferences = AppPreferences.Load(AppPreferences.DefaultPath);
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build();
        var updates = new AppUpdates(new VelopackUpdateClient(UpdateSettings.SourceIn(configuration)));
        var clientFiles = new ClientFiles(log => new InstalledClientSteps(log));
        var host = new HostRunner(args, preferences.Host);

        host.Start();
        _ = updates.CheckAsync();

        try
        {
            return BuildAvaloniaApp(host, preferences, updates, clientFiles).StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Off the UI's thread, which no longer runs: the bots stop, and the clients get their configuration back (D35).
            Task.Run(host.StopAsync).GetAwaiter().GetResult();

            // Last: installing the version downloaded may end the process at once, and starts the app again when asked (D173).
            updates.Finish();

            // Once the host stopped, which held the relay's address: the app started again reads the client's files prepared (D440).
            if (clientFiles.RestartRequested && !updates.RestartRequested)
                ClientFiles.StartAgain(args);
        }
    }

    private static AppBuilder BuildAvaloniaApp(HostRunner host, AppPreferences preferences, AppUpdates updates, ClientFiles clientFiles)
    {
        return App.Configure(AppBuilder.Configure(() => new App { Host = host, Preferences = preferences, Updates = updates, ClientFiles = clientFiles }))
            .UsePlatformDetect()
            .LogToTrace();
    }
}
