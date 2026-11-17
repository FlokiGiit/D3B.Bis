using D3B.Bis.Data.Packs;
using D3B.Bis.Extraction;
using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.App.Tests.Updates;

/// <summary>Steps of preparing a build's files that do what the test sets, without a client, and say a line each in the log.</summary>
internal sealed class FakePreparation : IPreparationSteps
{
    private readonly TextWriter _log;
    private readonly FakePreparationScript _script;

    public FakePreparation(TextWriter log, FakePreparationScript script)
    {
        _log = log;
        _script = script;
    }

    public void ExtractPack(string clientDirectory, string root, IProgress<double> progress)
    {
        _script.Calls.Add($"pack {clientDirectory}");
        _log.WriteLine("Pack: read from the fake client");
        progress.Report(0.5);

        // The test looks at the banner while the step runs, then lets it go on.
        _script.Gate?.Wait(TestContext.Current.CancellationToken);

        if (_script.PackThrows is not null)
            throw _script.PackThrows;

        Write(root, PackLocations.FileName);
    }

    public void WriteSchema(string clientDirectory, string root)
    {
        _script.Calls.Add($"schema {clientDirectory}");

        if (_script.SchemaThrows is not null)
            throw _script.SchemaThrows;

        Write(root, SchemaFile.FileName);
    }

    public bool WriteMapping(string build, string root)
    {
        _script.Calls.Add($"mapping {build}");
        _log.WriteLine("  failure: d3b.game.MapChangeEvent has no counterpart");

        if (_script.MappingHolds)
            Write(root, MappingFile.FileName);

        return _script.MappingHolds;
    }

    private static void Write(string root, string name)
    {
        // Where the real steps write: one directory per build under the root they are given.
        Directory.CreateDirectory(Path.Combine(root, FakePreparationScript.Build));
        File.WriteAllText(Path.Combine(root, FakePreparationScript.Build, name), name);
    }
}
