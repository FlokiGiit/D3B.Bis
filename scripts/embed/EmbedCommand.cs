using System.CommandLine;
using D3B.Bis.Data;
using D3B.Bis.Data.Packs;
using D3B.Bis.Data.Releases;
using D3B.Bis.Protocol.Mapping;
using D3B.Bis.Protocol.Translation;
using D3B.Bis.Scripts.Unity;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace D3B.Bis.Scripts.Embed;

/// <summary>The command that puts a client build's pack, schema and mapping beside a published app (D172).</summary>
internal static class EmbedCommand
{
    public static RootCommand Create()
    {
        var version = new Option<string>("--client-version")
        {
            Description = "The client build whose files the app ships, the ClientVersion property of src/D3B.Bis.App.",
            Required = true,
        };
        var output = new Option<DirectoryInfo>("--output")
        {
            Description = "The published app's data directory, beside its executable: the files go in a directory named after the build under it.",
            Required = true,
        };
        var packs = new Option<DirectoryInfo?>("--packs")
        {
            Description = @"Takes the files from this directory, one directory per client version, such as %LOCALAPPDATA%\D3B.Bis\data, instead of the build's release.",
        };
        var repository = new Option<string>("--repository")
        {
            Description = "The repository whose releases ship the files, as owner/name.",
            DefaultValueFactory = _ => ClientRelease.DefaultRepository,
        };
        var command = new RootCommand("Puts the pack, schema and mapping of a Dofus 3 client build beside the published D3B app.")
        {
            version,
            output,
            packs,
            repository,
        };

        command.SetAction(result => Run(
            result.GetValue(version)!,
            result.GetValue(output)!.FullName,
            result.GetValue(packs)?.FullName,
            result.GetValue(repository)!));

        return command;
    }

    private static int Run(string version, string output, string? packs, string repository)
    {
        ClientBuild build;

        try
        {
            build = new ClientBuild(version, null);
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);

            return 2;
        }

        if (!ClientRelease.IsRepository(repository))
        {
            Console.Error.WriteLine($"'{repository}' does not name a GitHub repository as owner/name.");

            return 1;
        }

        string[] names = [PackLocations.FileName, SchemaFile.FileName, MappingFile.FileName];
        var download = packs is null ? Directory.CreateTempSubdirectory("d3b-embed-") : null;

        try
        {
            var source = download?.FullName ?? Path.Combine(packs!, build.Version);

            if (download is not null)
                Download(build, repository, names, source);

            foreach (var name in names.Where(name => !File.Exists(Path.Combine(source, name))))
            {
                Console.Error.WriteLine($"No {name} for client {build.Version} in {source}.");

                return 2;
            }

            Check(build, source);

            var target = Path.Combine(output, build.Version);

            Directory.CreateDirectory(target);

            foreach (var name in names)
                File.Copy(Path.Combine(source, name), Path.Combine(target, name), true);

            Console.WriteLine($"Embedded {string.Join(", ", names)} of client {build.Version} in {target}.");

            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException
            or InvalidProtocolBufferException or DescriptorValidationException or ArgumentException or ProtocolMismatchException)
        {
            Console.Error.WriteLine($"The files of client {build.Version} cannot be embedded: {exception.Message}");

            return 1;
        }
        finally
        {
            download?.Delete(true);
        }
    }

    /// <summary>Downloads the files from the build's release, each checked against its SHA-256 (D24), with the app's own code (D440).</summary>
    private static void Download(ClientBuild build, string repository, string[] names, string directory)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        var downloader = new ClientReleaseDownloader(http, repository, Token());

        try
        {
            downloader.DownloadAsync(build, names, directory, null, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (ClientReleaseException exception)
        {
            throw new InvalidDataException(exception.Message, exception);
        }
    }

    /// <summary>Gets the token a private repository's releases need: the environment's, as CI gives it, else the GitHub CLI's.</summary>
    private static string? Token()
    {
        if (Environment.GetEnvironmentVariable("GH_TOKEN") is { Length: > 0 } token)
            return token;

        if (Environment.GetEnvironmentVariable("GITHUB_TOKEN") is { Length: > 0 } github)
            return github;

        try
        {
            var (exitCode, output, _) = GitHubCli.Run(["auth", "token"]);

            return exitCode is 0 && output.Trim() is { Length: > 0 } cli ? cli : null;
        }
        catch (InvalidOperationException)
        {
            // Without gh, a public repository's releases are read with no token.
            return null;
        }
    }

    /// <summary>Checks the files as the host does before it starts: the pack is the build's, and the mapping holds (D31, D48).</summary>
    private static void Check(ClientBuild build, string directory)
    {
        PackInfo info;

        using (var pack = GamePack.Open(Path.Combine(directory, PackLocations.FileName)))
            info = pack.Info;

        if (info.ClientVersion != build.Version)
            throw new InvalidDataException($"its {PackLocations.FileName} was extracted from client {info.ClientVersion}.");

        var schema = ProtocolSchema.Load(Path.Combine(directory, SchemaFile.FileName));
        var mapping = MappingFile.Read(Path.Combine(directory, MappingFile.FileName));

        if (mapping.ClientVersion != build.Version)
            throw new InvalidDataException($"its {MappingFile.FileName} maps client {mapping.ClientVersion}.");

        var audit = ProtocolTranslation.Create(schema, mapping).Audit;

        Console.WriteLine($"Pack: format {GamePack.FormatVersion}, extractor {info.ExtractorVersion}, fingerprint {info.Fingerprint}");
        Console.WriteLine($"Mapping: format {MappingFile.FormatVersion}, tool {mapping.ToolVersion}; {audit.Messages} messages, {audit.Fields} fields, {audit.Enums} enums and {audit.Values} values audited against the schema");
    }
}
