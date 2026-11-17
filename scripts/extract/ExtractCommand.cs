using System.CommandLine;
using D3B.Bis.Data.Packs;
using D3B.Bis.Extraction.Packs;
using D3B.Bis.Extraction.Unity;

namespace D3B.Bis.Scripts.Extract;

/// <summary>The command that extracts the installed client's game data into the pack of its build.</summary>
internal static class ExtractCommand
{
    public static RootCommand Create()
    {
        var client = new Option<DirectoryInfo?>("--client")
        {
            Description = @"The client's directory. Defaults to %LOCALAPPDATA%\Ankama\Dofus-dofus3 on Windows.",
        };
        var output = new Option<DirectoryInfo?>("--output")
        {
            Description = @"The directory packs are kept in, one directory per client version. Defaults to %LOCALAPPDATA%\D3B.Bis\data, where the host looks first.",
        };
        var force = new Option<bool>("--force")
        {
            Description = "Extracts again even when the pack was made from the same files by the same extractor.",
        };
        var level = new Option<int>("--level")
        {
            Description = "The zstd compression level of the pack's entries.",
            DefaultValueFactory = _ => PackWriter.DefaultCompressionLevel,
        };
        var command = new RootCommand("Extracts the game data of the installed Dofus 3 client into the pack of its build.")
        {
            client,
            output,
            force,
            level,
        };

        command.SetAction(result => Run(
            result.GetValue(client),
            result.GetValue(output)?.FullName ?? PackLocations.DefaultRoot,
            result.GetValue(force),
            result.GetValue(level)));

        return command;
    }

    private static int Run(DirectoryInfo? clientDirectory, string root, bool force, int level)
    {
        ClientInstall client;

        try
        {
            client = ClientInstall.Locate(clientDirectory);
        }
        catch (DirectoryNotFoundException exception)
        {
            Console.Error.WriteLine(exception.Message);

            return 2;
        }

        try
        {
            PackExtraction.Extract(client, root, Console.Out, null, force, level);

            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The extraction failed: {exception.Message}");

            return 1;
        }
    }
}
