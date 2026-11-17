using System.CommandLine;
using D3B.Bis.Data.Packs;
using D3B.Bis.Extraction.Schema;
using D3B.Bis.Extraction.Unity;
using Google.Protobuf.Reflection;

namespace D3B.Bis.Scripts.Schema;

/// <summary>The command that reads the installed client's protocol and writes its obfuscated schema.</summary>
internal static class SchemaCommand
{
    public static RootCommand Create()
    {
        var client = new Option<DirectoryInfo?>("--client")
        {
            Description = @"The client's directory. Defaults to %LOCALAPPDATA%\Ankama\Dofus-dofus3 on Windows.",
        };
        var output = new Option<DirectoryInfo?>("--output")
        {
            Description = @"The directory schemas are kept in, one directory per client version, beside the packs. Defaults to %LOCALAPPDATA%\D3B.Bis\data.",
        };
        var proto = new Option<DirectoryInfo?>("--proto")
        {
            Description = "Also writes the schema as .proto text into this directory, to read it. Derived from the client: never commit it.",
        };
        var command = new RootCommand("Reads the protocol of the installed Dofus 3 client with Cpp2IL and writes its obfuscated schema.")
        {
            client,
            output,
            proto,
        };

        command.SetAction(result => Run(
            result.GetValue(client),
            result.GetValue(output)?.FullName ?? PackLocations.DefaultRoot,
            result.GetValue(proto)?.FullName));

        return command;
    }

    private static int Run(DirectoryInfo? clientDirectory, string root, string? protoDirectory)
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
            SchemaExtraction.Write(client, root, Console.Out, protoDirectory);

            return 0;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or DescriptorValidationException)
        {
            Console.Error.WriteLine($"The schema could not be read: {exception.Message}");

            return 1;
        }
    }
}
