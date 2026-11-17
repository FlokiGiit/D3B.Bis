using D3B.Bis.Data.Interactives;
using D3B.Bis.Extraction.Unity;

namespace D3B.Bis.Extraction.Packs;

/// <summary>Reads the kinds of interactive elements, which the server names an element of the map by.</summary>
internal static class InteractivesExtractor
{
    public static IReadOnlyList<string> Inputs(ClientInstall client)
    {
        return [UnityDataTable.PathOf(client, "interactives")];
    }

    public static InteractiveCatalog Read(ClientInstall client, TextWriter log)
    {
        try
        {
            var interactives = UnityDataTable.ReadById(client, "interactives").Select(record => new Interactive(record.Int32("id"), record.Int32("nameId")));
            var catalog = new InteractiveCatalog(interactives.OrderBy(interactive => interactive.Id).ToArray());

            log.WriteLine($"Interactives: {catalog.Interactives.Length:N0}");

            return catalog;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"The interactives' table does not fit what the extractor measured: {exception.Message}", exception);
        }
    }
}
