using D3B.Bis.Data.Tests;
using Microsoft.Data.Sqlite;

namespace D3B.Bis.Storage.Tests;

public sealed class PreviousInstallTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    private string Previous => _directory.Combine("D3B");

    private string Root => _directory.Combine("D3B.Bis");

    public void Dispose()
    {
        _directory.Dispose();
    }

    [Fact]
    public void AFirstStartBringsTheDatabaseTheProjectAndThePreferencesAcross()
    {
        WritePrevious();

        Assert.True(PreviousInstall.Import(Previous, Root));

        Assert.Equal("Iop", NameIn(Path.Combine(Root, "d3b.db")));
        Assert.Equal("preferences", File.ReadAllText(Path.Combine(Root, "app.json")));
        Assert.Equal("profile", File.ReadAllText(Path.Combine(Root, "studio", "Harvests", "wheat.profile.json")));
    }

    [Fact]
    public void WhatTheFolderAlreadyHoldsIsKept()
    {
        WritePrevious();

        // As when the packs were fetched before the first start: the folder is there, without a database.
        Directory.CreateDirectory(Path.Combine(Root, "data"));
        File.WriteAllText(Path.Combine(Root, "app.json"), "mine");

        Assert.True(PreviousInstall.Import(Previous, Root));

        Assert.Equal("mine", File.ReadAllText(Path.Combine(Root, "app.json")));
        Assert.Equal("Iop", NameIn(Path.Combine(Root, "d3b.db")));
        Assert.True(File.Exists(Path.Combine(Root, "studio", "Harvests", "wheat.profile.json")));
    }

    [Fact]
    public void AFolderThatHasItsDatabaseTakesNothing()
    {
        WritePrevious();
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "d3b.db"), "");

        Assert.False(PreviousInstall.Import(Previous, Root));

        Assert.Equal(["d3b.db"], Directory.EnumerateFileSystemEntries(Root).Select(Path.GetFileName));
    }

    [Fact]
    public void NothingIsMadeWithoutAPreviousDatabase()
    {
        Directory.CreateDirectory(Path.Combine(Previous, "studio"));
        File.WriteAllText(Path.Combine(Previous, "app.json"), "preferences");

        Assert.False(PreviousInstall.Import(Previous, Root));

        Assert.False(Directory.Exists(Root));
    }

    [Fact]
    public void ThePreviousFilesAreOnlyRead()
    {
        WritePrevious();

        var before = Snapshot(Previous);

        Assert.True(PreviousInstall.Import(Previous, Root));

        Assert.Equal(before, Snapshot(Previous));
    }

    [Fact]
    public void ADatabaseThatCannotBeReadLeavesNoDatabaseBehind()
    {
        Directory.CreateDirectory(Previous);
        File.WriteAllText(Path.Combine(Previous, "d3b.db"), "not a database, and long enough to be read as one: " + new string('x', 4096));

        Assert.False(PreviousInstall.Import(Previous, Root));

        Assert.Empty(Directory.Exists(Root) ? Directory.EnumerateFiles(Root) : []);
    }

    private static string? NameIn(string database)
    {
        using var connection = Open(database);
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT Name FROM Characters";

        return (string?)command.ExecuteScalar();
    }

    private static SqliteConnection Open(string database)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database, Pooling = false }.ToString());

        connection.Open();

        return connection;
    }

    private static List<(string Path, string Content)> Snapshot(string folder)
    {
        // But for the -wal and -shm a read of a database in WAL mode leaves beside it, empty of changes, which its next writer clears.
        return [.. Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Where(file => !file.EndsWith("-wal", StringComparison.Ordinal) && !file.EndsWith("-shm", StringComparison.Ordinal)).Order(StringComparer.Ordinal).Select(file => (Path.GetRelativePath(folder, file), Convert.ToHexString(File.ReadAllBytes(file))))];
    }

    private void WritePrevious()
    {
        Directory.CreateDirectory(Path.Combine(Previous, "studio", "Harvests"));
        File.WriteAllText(Path.Combine(Previous, "app.json"), "preferences");
        File.WriteAllText(Path.Combine(Previous, "studio", "Harvests", "wheat.profile.json"), "profile");

        using var connection = Open(Path.Combine(Previous, "d3b.db"));
        using var command = connection.CreateCommand();

        // In WAL mode, as EF Core makes D3B's database.
        command.CommandText = "PRAGMA journal_mode = WAL; CREATE TABLE Characters (Name TEXT); INSERT INTO Characters VALUES ('Iop')";
        command.ExecuteNonQuery();
    }
}
