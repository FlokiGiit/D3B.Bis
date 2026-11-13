using Microsoft.Data.Sqlite;

namespace D3B.Bis.Storage;

/// <summary>What D3B kept on this machine before D3B.Bis had a folder of its own, brought across once.</summary>
/// <remarks>
/// <para>
/// D3B.Bis keeps its files in <c>%LOCALAPPDATA%\D3B.Bis</c>, apart from D3B's <c>%LOCALAPPDATA%\D3B</c>: D3B may still be
/// installed, runs its own migrations on its database, and its new installer writes in that folder. At the first start,
/// the one where D3B.Bis has no database yet, the database, the studio's project and the app's preferences are copied,
/// each only where D3B.Bis has none; their formats are the ones D3B wrote. D3B's files are only read: the read of its
/// database, in WAL mode, may leave SQLite's empty <c>-wal</c> and <c>-shm</c> beside it, which D3B's next write clears.
/// </para>
/// <para>
/// The captures stay where they are: they hold login tickets and are read by path. So do the packs, which are fetched
/// again, and the client's configuration, which the host writes at each start.
/// </para>
/// </remarks>
public static class PreviousInstall
{
    private const string Database = "d3b.db";

    // The app's preferences, whose name D3B.Bis.App holds: the app is above this project, and the host imports too.
    private const string Preferences = "app.json";

    private const string Studio = "studio";

    /// <summary>Gets D3B's folder: <c>%LOCALAPPDATA%\D3B</c>.</summary>
    public static string PreviousRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3B");

    /// <summary>Gets D3B.Bis's folder: <c>%LOCALAPPDATA%\D3B.Bis</c>, where <see cref="StorageSettings.DefaultPath"/> is.</summary>
    public static string Root => Path.GetDirectoryName(StorageSettings.DefaultPath)!;

    /// <summary>Copies what a previous folder holds into a folder that has no database yet.</summary>
    /// <param name="previousRoot">The folder D3B kept its files in, such as <see cref="PreviousRoot"/>.</param>
    /// <param name="root">The folder D3B.Bis keeps its files in, such as <see cref="Root"/>.</param>
    /// <returns>Whether the database was brought across; false when there was none to bring, one already, or it could not be read.</returns>
    public static bool Import(string previousRoot, string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(previousRoot);
        ArgumentException.ThrowIfNullOrEmpty(root);

        var source = Path.Combine(previousRoot, Database);
        var target = Path.Combine(root, Database);

        if (File.Exists(target) || !File.Exists(source))
            return false;

        // Beside its place, then moved: a copy cut short leaves no database, and the next start tries again.
        var copy = target + ".import";

        try
        {
            Directory.CreateDirectory(root);
            File.Delete(copy);
            Backup(source, copy);
            CopyMissing(Path.Combine(previousRoot, Preferences), Path.Combine(root, Preferences));

            var studio = Path.Combine(previousRoot, Studio);

            if (Directory.Exists(studio))
            {
                foreach (var file in Directory.EnumerateFiles(studio, "*", SearchOption.AllDirectories))
                    CopyMissing(file, Path.Combine(root, Studio, Path.GetRelativePath(studio, file)));
            }

            File.Move(copy, target);

            return true;
        }
        catch (Exception exception) when (exception is SqliteException or IOException or UnauthorizedAccessException)
        {
            File.Delete(copy);

            return false;
        }
    }

    private static void Backup(string source, string target)
    {
        // SQLite's own copy, not the file's: D3B may be running and writing its database at this moment.
        using var from = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = source, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        using var to = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = target, Pooling = false }.ToString());

        from.Open();
        to.Open();
        from.BackupDatabase(to);
    }

    private static void CopyMissing(string source, string target)
    {
        if (!File.Exists(source) || File.Exists(target))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
    }
}
