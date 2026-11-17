using D3B.Bis.Data;
using D3B.Bis.Extraction.Mapping;
using D3B.Bis.Extraction.Packs;
using D3B.Bis.Extraction.Schema;
using D3B.Bis.Extraction.Unity;
using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.Extraction;

/// <summary>The steps of preparing a build's files from the installed client, as the tools of <c>scripts/</c> take them (D440).</summary>
/// <remarks>
/// The mapping is made as the owner's launcher made it: from the pins committed for the build, else the latest build's
/// carried over to it, and without captures, which a player's machine may not hold for a build just released.
/// </remarks>
public sealed class InstalledClientSteps : IPreparationSteps
{
    private readonly TextWriter _log;

    /// <summary>Initializes a new instance of the <see cref="InstalledClientSteps"/> class.</summary>
    /// <param name="log">Where each step says what it read and wrote, line by line.</param>
    public InstalledClientSteps(TextWriter log)
    {
        ArgumentNullException.ThrowIfNull(log);

        _log = log;
    }

    /// <inheritdoc/>
    public void ExtractPack(string clientDirectory, string root, IProgress<double> progress)
    {
        PackExtraction.Extract(ClientInstall.Locate(new DirectoryInfo(clientDirectory)), root, _log, progress);
    }

    /// <inheritdoc/>
    public void WriteSchema(string clientDirectory, string root)
    {
        SchemaExtraction.Write(ClientInstall.Locate(new DirectoryInfo(clientDirectory)), root, _log);
    }

    /// <inheritdoc/>
    public bool WriteMapping(string build, string root)
    {
        var client = new ClientBuild(build, null);
        var schemaPath = SchemaFile.PathOf(root, client.Version);
        var schema = ProtocolSchema.Load(schemaPath);
        var pins = CommittedPins.For(client.Version);

        _log.WriteLine($"Client {client.Version}; schema {schemaPath}, SHA-256 {Convert.ToHexStringLower([.. schema.Sha256])}");
        _log.WriteLine($"Pins: those committed for {CommittedPins.SourceOf(client.Version)}, {pins.Messages.Count} messages and {pins.Enums.Count} enums");
        _log.WriteLine("Captures: not read; matching from pins and structure alone");

        return MappingMaker.Make(client, root, schema, pins, [], [], _log).Holds;
    }
}
