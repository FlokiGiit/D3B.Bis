using System.Diagnostics;
using System.Security.Cryptography;
using D3B.Bis.Data;
using D3B.Bis.Extraction.Unity;
using D3B.Bis.Protocol.Mapping;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace D3B.Bis.Extraction.Schema;

/// <summary>Reads the protocol of an installed client from its IL2CPP code and writes its obfuscated schema (D11, D15), for the tools and the app (D440).</summary>
public static class SchemaExtraction
{
    private static readonly (string Assembly, string File)[] _protocols =
    [
        ("Ankama.Dofus.Protocol.Connection", "connection.proto"),
        ("Ankama.Dofus.Protocol.Game", "game.proto"),
    ];

    // Cpp2IL keeps the client it opened in static state: two reads at once in one process would read each other's.
    private static readonly Lock _cpp2il = new();

    /// <summary>Writes the schema of the client's build into its directory under a root.</summary>
    /// <param name="client">The installed client.</param>
    /// <param name="root">The directory schemas are kept in, one directory per client version, beside the packs.</param>
    /// <param name="log">Where what was read and written is said, line by line.</param>
    /// <param name="protoDirectory">A directory to write the schema into as .proto text too, or <see langword="null"/>.</param>
    /// <returns>The path of the schema.</returns>
    /// <exception cref="IOException">A file of the client or of the schema could not be read or written.</exception>
    /// <exception cref="InvalidDataException">The client's code does not read as protobuf's generated code: nothing is written.</exception>
    /// <exception cref="DescriptorValidationException">What was read is no schema protobuf loads: nothing is written.</exception>
    /// <exception cref="UnauthorizedAccessException">The schema's directory refused the write.</exception>
    public static string Write(ClientInstall client, string root, TextWriter log, string? protoDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(log);

        lock (_cpp2il)
            return WriteAlone(client, root, log, protoDirectory);
    }

    private static string WriteAlone(ClientInstall client, string root, TextWriter log, string? protoDirectory)
    {
        var stopwatch = Stopwatch.StartNew();
        var build = ClientBuild.Read(client.Root);

        log.WriteLine($"Client: {client.Root}, version {build}");

        using var code = ClientCode.Open(client);

        log.WriteLine($"Unity {code.UnityVersion}, IL2CPP metadata version {code.MetadataVersion}, opened in {stopwatch.Elapsed.TotalSeconds:F1} s");

        var reading = Stopwatch.StartNew();
        var files = _protocols.Select(protocol => ProtocolReader.Read(code.Assembly(protocol.Assembly), protocol.File)).ToList();
        var set = new FileDescriptorSet();

        if (files.Any(file => file.Dependency.Contains(ProtocolTypes.AnyFile)))
            set.File.Add(AnyReflection.Descriptor.ToProto());

        set.File.AddRange(files);

        // Building the descriptors links every type name to its type and refuses a name declared twice: a schema protobuf
        // cannot load is never written.
        FileDescriptor.BuildFromByteStrings(set.File.Select(file => file.ToByteString()));

        log.WriteLine($"Read in {reading.Elapsed.TotalSeconds:F1} s");

        var types = new SortedDictionary<string, int>(StringComparer.Ordinal);

        foreach (var (file, protocol) in files.Zip(_protocols))
        {
            var summary = SchemaSummary.Of(file);

            log.WriteLine($"{file.Name}, from {protocol.Assembly}: {summary}");

            foreach (var (type, count) in summary.Types)
                types[type] = types.GetValueOrDefault(type) + count;
        }

        log.WriteLine($"Field types: {string.Join(", ", types.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}"))}");

        var path = SchemaFile.PathOf(root, build.Version);
        var bytes = set.ToByteArray();
        var temporary = path + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written aside, then moved: a schema is never half written under its name.
        File.WriteAllBytes(temporary, bytes);
        File.Move(temporary, path, true);

        log.WriteLine($"Schema: {path}, {bytes.Length:N0} bytes, format {SchemaFile.FormatVersion}, in {stopwatch.Elapsed.TotalSeconds:F1} s");
        log.WriteLine($"SHA-256: {Convert.ToHexStringLower(SHA256.HashData(bytes))}");

        if (protoDirectory is not null)
        {
            Directory.CreateDirectory(protoDirectory);

            foreach (var (file, protocol) in files.Zip(_protocols))
            {
                using var writer = new StreamWriter(Path.Combine(protoDirectory, file.Name));

                ProtoText.Write(writer, file, $"The obfuscated protocol of client {build.Version}, read from {protocol.Assembly} by scripts/schema.cs. Never commit it.");
            }

            log.WriteLine($"Text: {string.Join(", ", files.Select(file => Path.Combine(protoDirectory, file.Name)))}");
        }

        return path;
    }
}
