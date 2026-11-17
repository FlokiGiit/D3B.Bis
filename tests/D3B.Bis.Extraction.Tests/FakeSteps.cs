using D3B.Bis.Data.Packs;
using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.Extraction.Tests;

/// <summary>Steps that record what they are asked, write a file each as the real ones do, and do what the test sets, without a client.</summary>
internal sealed class FakeSteps : IPreparationSteps
{
    public required string Build { get; init; }

    public List<string> Calls { get; } = [];

    public IReadOnlyList<double> PackProgress { get; init; } = [];

    public Exception? PackThrows { get; init; }

    public Exception? SchemaThrows { get; init; }

    public Exception? MappingThrows { get; init; }

    public bool MappingHolds { get; init; } = true;

    public void ExtractPack(string clientDirectory, string root, IProgress<double> progress)
    {
        Calls.Add($"pack {clientDirectory}");

        foreach (var share in PackProgress)
            progress.Report(share);

        if (PackThrows is not null)
            throw PackThrows;

        Write(root, PackLocations.FileName);
    }

    public void WriteSchema(string clientDirectory, string root)
    {
        Calls.Add($"schema {clientDirectory}");

        if (SchemaThrows is not null)
            throw SchemaThrows;

        Write(root, SchemaFile.FileName);
    }

    public bool WriteMapping(string build, string root)
    {
        Calls.Add($"mapping {build}");

        if (MappingThrows is not null)
            throw MappingThrows;

        if (MappingHolds)
            Write(root, MappingFile.FileName);

        return MappingHolds;
    }

    private void Write(string root, string name)
    {
        // Where the real steps write: one directory per build under the root they are given.
        Directory.CreateDirectory(Path.Combine(root, Build));
        File.WriteAllText(Path.Combine(root, Build, name), $"the {name} of {Build}");
    }
}
