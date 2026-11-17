using System.Buffers;
using System.CommandLine;
using System.Globalization;
using D3B.Bis.Data;
using D3B.Bis.Data.Packs;
using D3B.Bis.Extraction.Mapping;
using D3B.Bis.Extraction.Unity;
using D3B.Bis.Network.Relay;
using D3B.Bis.Protocol.Mapping;
using D3B.Bis.Protocol.Translation;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace D3B.Bis.Scripts.Mapping;

/// <summary>The command that matches D3B's readable protocol with a build's schema, audits the result and writes the mapping.</summary>
internal static class MappingCommand
{
    public static RootCommand Create()
    {
        var client = new Option<DirectoryInfo?>("--client")
        {
            Description = @"The client whose build to map. Defaults to %LOCALAPPDATA%\Ankama\Dofus-dofus3 on Windows.",
        };
        var version = new Option<string?>("--client-version")
        {
            Description = "The client version to map, instead of the installed client's.",
        };
        var data = new Option<DirectoryInfo?>("--data")
        {
            Description = @"The directory packs, schemas and mappings are kept in, one directory per client version. Defaults to %LOCALAPPDATA%\D3B.Bis\data.",
        };
        var pins = new Option<DirectoryInfo?>("--pins")
        {
            Description = "The directory of pinned pairs, one <client version>.json per build. Defaults to src/D3B.Bis.Protocol/Pins.",
        };
        var captures = new Option<string[]>("--captures")
        {
            Description = @"Captures of the build, which the anchors are measured in: directories as the host writes them, the game's in their game directory, or files. Defaults to %LOCALAPPDATA%\D3B.Bis\captures.",
            AllowMultipleArgumentsPerToken = true,
        };
        var withoutCaptures = new Option<bool>("--without-captures")
        {
            Description = "Matches without anchors, from pins and structure alone: measures how far they go.",
        };
        var json = new Option<bool>("--json")
        {
            Description = "Also prints the mapping as JSON, to read it. Derived from the client: never commit it.",
        };
        var command = new RootCommand("Matches D3B's readable protocol with a Dofus 3 build's obfuscated schema and writes the build's mapping.")
        {
            client,
            version,
            data,
            pins,
            captures,
            withoutCaptures,
            json,
        };

        command.SetAction(result => Run(
            result.GetValue(client),
            result.GetValue(version),
            result.GetValue(data)?.FullName ?? PackLocations.DefaultRoot,
            result.GetValue(pins)?.FullName ?? DefaultPins(),
            result.GetValue(withoutCaptures) ? [] : result.GetValue(captures) is { Length: > 0 } paths ? paths : [RelaySettings.DefaultCapturesDirectory],
            result.GetValue(json)));

        return command;
    }

    private static string DefaultPins()
    {
        var scripts = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Path.Combine(Directory.GetCurrentDirectory(), "scripts");

        return Path.GetFullPath(Path.Combine(scripts, "..", "src", "D3B.Bis.Protocol", "Pins"));
    }

    private static int Run(DirectoryInfo? clientDirectory, string? version, string root, string pinsDirectory, string[] capturePaths, bool json)
    {
        ClientBuild build;

        try
        {
            build = version is null ? ClientBuild.Read(ClientInstall.Locate(clientDirectory).Root) : new ClientBuild(version, null);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);

            return 2;
        }

        var schemaPath = SchemaFile.PathOf(root, build.Version);

        if (!File.Exists(schemaPath))
        {
            Console.Error.WriteLine($"No schema of client {build.Version} in {schemaPath}: write it with dotnet run --file scripts/schema.cs.");

            return 2;
        }

        // Without its directory, the pins a build needs would go unread, and a field they pair be paired by its number.
        if (!Directory.Exists(pinsDirectory))
        {
            Console.Error.WriteLine($"No directory of pins in {pinsDirectory}: name it with --pins.");

            return 2;
        }

        try
        {
            return Map(build, root, schemaPath, pinsDirectory, capturePaths, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or InvalidProtocolBufferException or DescriptorValidationException or ArgumentException)
        {
            Console.Error.WriteLine($"The mapping could not be made: {exception.Message}");

            return 1;
        }
    }

    private static int Map(ClientBuild build, string root, string schemaPath, string pinsDirectory, string[] capturePaths, bool json)
    {
        var schema = ProtocolSchema.Load(schemaPath);
        var pinsPath = PinnedPairsFile.PathOf(pinsDirectory, build.Version);
        var pins = File.Exists(pinsPath) ? PinnedPairsFile.Read(pinsPath, build.Version) : null;
        var failed = false;

        Console.WriteLine($"Client {build.Version}; schema {schemaPath}, SHA-256 {Convert.ToHexStringLower([.. schema.Sha256])}");
        Console.WriteLine(pins is null
            ? $"Pins: none, no {pinsPath}"
            : $"Pins: {pinsPath}, {pins.Messages.Count} messages and {pins.Enums.Count} enums");

        var anchors = new List<Anchor>();
        var payloads = new List<CapturedPayload>();
        var game = new List<CapturedConnection>();

        if (capturePaths.Length is 0)
        {
            Console.WriteLine("Captures: not read; matching from pins and structure alone");
        }
        else
        {
            var (connection, captured) = CaptureFrames.Read(capturePaths, build.Version, out var skipped);

            Console.WriteLine($"Captures: {connection.Count} of the connection phase and {captured.Count} of the game, of client {build.Version}; {skipped} skipped");
            failed |= !Measure("Connection phase", connection, schema, ReadableProtocol.ConnectionSchemaFile, ReadableProtocol.ConnectionAnchors, anchors, payloads);
            failed |= !Measure("Game", captured, schema, ReadableProtocol.GameSchemaFile, ReadableProtocol.GameAnchors, anchors, payloads);
            game = captured;
        }

        var outcome = MappingMaker.Make(build, root, schema, pins, anchors, payloads, Console.Out, !failed, json);

        if (!outcome.Holds)
        {
            Console.Error.WriteLine("The mapping does not hold, and was not written: pin what the structure leaves open, or fix the readable .proto files.");

            return 1;
        }

        if (game.Count > 0)
            Translate(ProtocolTranslation.Create(schema, outcome.Mapping), game);

        return 0;
    }

    /// <summary>Measures the anchors of one connection's captures, and says whether nothing contradicts the plan.</summary>
    private static bool Measure(string title, List<CapturedConnection> connections, ProtocolSchema schema, string fileName, AnchorPlan plan, List<Anchor> anchors, List<CapturedPayload> payloads)
    {
        var file = schema.FindFile(fileName) ?? throw new InvalidDataException($"The schema has no {fileName}.");
        var measurement = CaptureAnchors.Measure(connections, file, plan);

        Console.WriteLine($"  {title}: {connections.Count} captures, {measurement.Frames} frames");

        if (measurement.Root is not null)
        {
            Console.WriteLine($"    root: .{measurement.Root.FullName} reads every frame and types {measurement.RootTyped} fields, the next {(measurement.NextTyped < 0 ? "none" : measurement.NextTyped.ToString(CultureInfo.InvariantCulture))}");
            Console.WriteLine($"    anchors: {measurement.Anchors.Count} messages, {measurement.Anchors.Sum(anchor => anchor.Fields.Count)} fields: {string.Join(", ", measurement.Anchors.Select(anchor => anchor.Readable.Name))}");
        }

        if (measurement.Payloads.Count > 0)
            Console.WriteLine($"    carried: {measurement.Payloads.Count} messages in an Any, {measurement.Payloads.Count(payload => payload.FromClient > 0)} by the client and {measurement.Payloads.Count(payload => payload.FromServer > 0)} by the server, each read whole as its type URL names it");

        foreach (var problem in measurement.Problems)
            Console.WriteLine($"    problem: {problem}");

        anchors.AddRange(measurement.Anchors);
        payloads.AddRange(measurement.Payloads);

        // No frame means no anchor, which matching does without; a frame contradicting the plan means the plan or the
        // client changed, and must be looked at before a mapping is trusted.
        return measurement.Frames is 0 || measurement.Problems.Count is 0;
    }

    /// <summary>Decodes every frame of the game's captures through the mapping just written, as the host would (D47).</summary>
    private static void Translate(ProtocolTranslation translation, List<CapturedConnection> game)
    {
        var frames = game.SelectMany(connection => connection.ClientFrames.Concat(connection.ServerFrames)).ToList();
        var decoded = frames.Select(frame => translation.DecodeGame(new ReadOnlySequence<byte>(frame))).ToList();
        var understood = decoded.Count(frame => frame.IsUnderstood);
        var read = decoded.Count(frame => frame.IsRead && !frame.IsUnderstood);
        var lacking = decoded.Where(frame => frame.CarriedType is not null && frame.ReadableType is null).Select(frame => frame.CarriedType).ToList();
        var misread = decoded.Where(frame => !frame.IsUnderstood && !frame.IsRead && (frame.CarriedType is null || frame.ReadableType is not null)).ToList();

        Console.WriteLine($"Translation: {frames.Count} game frames, {understood} understood whole, {read} more read, {lacking.Count} carrying one of {lacking.Distinct().Count()} messages the readable protocol lacks, {misread.Count} not understood");

        foreach (var problem in misread.Select(frame => frame.Problem).Distinct().Take(10))
            Console.WriteLine($"  not understood: {problem}");
    }
}
