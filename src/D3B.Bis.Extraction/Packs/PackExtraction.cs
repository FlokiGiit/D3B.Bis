using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using D3B.Bis.Data;
using D3B.Bis.Data.Banks;
using D3B.Bis.Data.Hints;
using D3B.Bis.Data.Npcs;
using D3B.Bis.Data.Breeds;
using D3B.Bis.Data.Characteristics;
using D3B.Bis.Data.Effects;
using D3B.Bis.Data.Icons;
using D3B.Bis.Data.Interactives;
using D3B.Bis.Data.Items;
using D3B.Bis.Data.Jobs;
using D3B.Bis.Data.Maps;
using D3B.Bis.Data.Monsters;
using D3B.Bis.Data.Packs;
using D3B.Bis.Data.Spells;
using D3B.Bis.Data.Texts;
using D3B.Bis.Data.World;
using D3B.Bis.Data.WorldMaps;
using D3B.Bis.Extraction.Unity;

namespace D3B.Bis.Extraction.Packs;

/// <summary>Extracts the game data of an installed client into the pack of its build (D10, D24), for the tools and the app (D440).</summary>
public static class PackExtraction
{
    // Measured on client 3.7.14.18: 32 s of reading, then 25 s of writing; the progress weighs both alike.
    private const double ReadingShare = 0.55;

    /// <summary>Extracts the pack of the client's build into its directory under a root, unless it is up to date.</summary>
    /// <param name="client">The installed client.</param>
    /// <param name="root">The directory packs are kept in, one directory per client version.</param>
    /// <param name="log">Where what was read and written is said, line by line.</param>
    /// <param name="progress">Told the share done, from 0 to 1, or <see langword="null"/>.</param>
    /// <param name="force">Whether to extract again a pack made from the same files by the same extractor.</param>
    /// <param name="level">The zstd compression level of the pack's entries.</param>
    /// <returns>The path of the pack.</returns>
    /// <exception cref="IOException">A file of the client or of the pack could not be read or written.</exception>
    /// <exception cref="InvalidDataException">The client does not read as the extractor expects: nothing is written.</exception>
    /// <exception cref="UnauthorizedAccessException">The pack's directory refused the write.</exception>
    public static string Extract(ClientInstall client, string root, TextWriter log, IProgress<double>? progress = null, bool force = false, int level = PackWriter.DefaultCompressionLevel)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(log);

        var stopwatch = Stopwatch.StartNew();
        var build = ClientBuild.Read(client.Root);
        var manifest = LauncherManifest.Load(client);
        var inputs = TextsExtractor.Inputs(client)
            .Concat(MapsExtractor.Inputs(client))
            .Concat(WorldExtractor.Inputs(client))
            .Concat(ItemsExtractor.Inputs(client))
            .Concat(JobsExtractor.Inputs(client))
            .Concat(InteractivesExtractor.Inputs(client))
            .Concat(BanksExtractor.Inputs(client))
            .Concat(NpcsExtractor.Inputs(client))
            .Concat(HintsExtractor.Inputs(client))
            .Concat(IconsExtractor.Inputs(client))
            .Concat(EffectsExtractor.Inputs(client))
            .Concat(CharacteristicsExtractor.Inputs(client))
            .Concat(SpellsExtractor.Inputs(client))
            .Concat(StatesExtractor.Inputs(client))
            .Concat(MonstersExtractor.Inputs(client))
            .Concat(BreedsExtractor.Inputs(client))
            .Concat(WorldMapsExtractor.Inputs(client))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var fingerprint = InputFingerprint.Compute(client, manifest, inputs);
        var extractor = typeof(GamePack).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var path = PackLocations.PathOf(root, build);

        log.WriteLine($"Client: {client.Root}, version {build}");

        if (!force && IsUpToDate(path, fingerprint, extractor))
        {
            log.WriteLine($"Up to date: {path} was made from the same files by the same extractor. --force extracts again.");
            progress?.Report(1);

            return path;
        }

        // Everything is read and checked before the pack is opened, so that a client the extractor misreads leaves no file.
        var reading = Stopwatch.StartNew();
        var read = new Steps(progress, 0, ReadingShare, 19);
        var maps = read.Next(MapsExtractor.Read(client, log));
        var graph = read.Next(WorldExtractor.ReadGraph(client, log));
        var effects = read.Next(EffectsExtractor.Read(client, log));
        var (items, weapons) = read.Next(ItemsExtractor.Read(client, effects, log));
        var jobs = read.Next(JobsExtractor.Read(client, items, log));
        var characteristics = read.Next(CharacteristicsExtractor.Read(client, TextsExtractor.Held(client), log));
        var spells = read.Next(SpellsExtractor.Read(client, effects, log));
        var states = read.Next(StatesExtractor.Read(client, spells, log));
        var (monsters, monsterSubAreas) = read.Next(MonstersExtractor.Read(client, spells, log));
        var breeds = read.Next(BreedsExtractor.Read(client, spells, characteristics, log));
        var atlas = read.Next(WorldExtractor.ReadAtlas(client, items, jobs, monsters, monsterSubAreas, log));
        var interactives = read.Next(InteractivesExtractor.Read(client, log));
        var banks = read.Next(BanksExtractor.Read(client, atlas, graph, log));
        var npcs = read.Next(NpcsExtractor.Read(client, TextsExtractor.Held(client), log));
        var hints = read.Next(HintsExtractor.Read(client, TextsExtractor.Held(client), log));
        var icons = read.Next(IconsExtractor.Read(client, items, jobs, spells, breeds, monsters, atlas, log));
        var fights = new FightTables(spells, states, effects, monsters, breeds, characteristics);
        var (worldMaps, worldMapTiles) = read.Next(WorldMapsExtractor.Read(client, log));

        WorldExtractor.Check(graph, atlas, maps, log);
        read.Next(0);

        var texts = read.Next(TextsExtractor.Read(client, TextsExtractor.Named(atlas, items, jobs, interactives, banks, npcs, hints, fights, worldMaps), log));

        log.WriteLine($"Read in {reading.Elapsed.TotalSeconds:F1} s");

        var info = new PackInfo
        {
            ClientVersion = build.Version,
            ClientBuildDate = build.BuildDate,
            Fingerprint = fingerprint,
            ExtractorVersion = extractor,
        };
        var temporary = path + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var writing = Stopwatch.StartNew();
        var written = new Steps(progress, ReadingShare, 1 - ReadingShare, texts.Count + 17 + maps.Count + worldMapTiles.Count + 1);

        // Written aside, then moved: the host never sees a pack half written under the name it looks for.
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new PackWriter(stream, info, level))
        {
            foreach (var table in texts)
                written.Add(writer, TextTable.EntryName(table.Language), table);

            written.Add(writer, WorldAtlas.EntryName, atlas);
            written.Add(writer, WorldGraph.EntryName, graph);
            written.Add(writer, ItemCatalog.EntryName, items);
            written.Add(writer, WeaponCatalog.EntryName, weapons);
            written.Add(writer, JobCatalog.EntryName, jobs);
            written.Add(writer, InteractiveCatalog.EntryName, interactives);
            written.Add(writer, BankCatalog.EntryName, banks);
            written.Add(writer, NpcCatalog.EntryName, npcs);
            written.Add(writer, IconCatalog.EntryName, icons);
            written.Add(writer, HintCatalog.EntryName, hints);
            written.Add(writer, SpellCatalog.EntryName, spells);
            written.Add(writer, SpellStateCatalog.EntryName, states);
            written.Add(writer, EffectCatalog.EntryName, effects);
            written.Add(writer, MonsterCatalog.EntryName, monsters);
            written.Add(writer, BreedCatalog.EntryName, breeds);
            written.Add(writer, CharacteristicCatalog.EntryName, characteristics);
            written.Add(writer, WorldMapCatalog.EntryName, worldMaps);

            foreach (var map in maps)
                written.Add(writer, MapData.EntryName(map.Id), map);

            // Already JPEG files: zstd finds nothing to take out, so each tile is its own small frame, read alone.
            foreach (var (name, jpeg) in worldMapTiles)
                written.Add(writer, name, jpeg);

            writer.Complete();
            written.Next(0);
        }

        log.WriteLine($"Written in {writing.Elapsed.TotalSeconds:F1} s");

        File.Move(temporary, path, true);

        using var pack = File.OpenRead(path);

        log.WriteLine($"Pack: {path}, {pack.Length:N0} bytes, format {GamePack.FormatVersion}, extracted in {stopwatch.Elapsed.TotalSeconds:F1} s");
        log.WriteLine($"Fingerprint: {fingerprint}");
        log.WriteLine($"SHA-256: {Convert.ToHexStringLower(SHA256.HashData(pack))}");

        return path;
    }

    private static bool IsUpToDate(string path, string fingerprint, string extractor)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using var existing = GamePack.Open(path);

            return existing.Info.Fingerprint == fingerprint && existing.Info.ExtractorVersion == extractor;
        }
        catch (PackFormatException)
        {
            // A pack this extractor cannot read is replaced.
            return false;
        }
    }

    /// <summary>Counts the steps of one part of the extraction into its share of the progress.</summary>
    private sealed class Steps
    {
        private readonly IProgress<double>? _progress;
        private readonly double _start;
        private readonly double _share;
        private readonly int _count;
        private int _done;

        public Steps(IProgress<double>? progress, double start, double share, int count)
        {
            _progress = progress;
            _start = start;
            _share = share;
            _count = count;
        }

        public T Next<T>(T value)
        {
            _done = Math.Min(_done + 1, _count);
            _progress?.Report(_start + (_share * _done / _count));

            return value;
        }

        public void Add<T>(PackWriter writer, string name, T value)
        {
            writer.Add(name, value);
            Next(0);
        }
    }
}
