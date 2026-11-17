using System.Collections.Frozen;
using Google.Protobuf;

namespace D3B.Bis.Protocol.Mapping;

/// <summary>The pinned pairs committed with the readable protocol, carried by its assembly so that an installed app reads them (D43, D440).</summary>
/// <remarks>
/// A build Ankama released after the last pins were committed takes the latest build's: the obfuscated names a pin gives
/// stayed the same across the builds measured so far, and the mapping's audit refuses pins that no longer hold.
/// </remarks>
public static class CommittedPins
{
    private const string Prefix = "Pins.";

    private static readonly FrozenDictionary<string, string> _resources = typeof(CommittedPins).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
        .ToFrozenDictionary(name => name[Prefix.Length..^".json".Length], StringComparer.Ordinal);

    /// <summary>Gets the builds whose pins are committed, oldest first.</summary>
    public static IReadOnlyList<string> Builds { get; } = Ordered(_resources.Keys);

    /// <summary>Gets the build whose pins <see cref="For"/> reads for a build: its own when committed, else the latest committed.</summary>
    /// <param name="clientVersion">The build mapped.</param>
    /// <returns>The build whose pins are read.</returns>
    /// <exception cref="ArgumentException"><paramref name="clientVersion"/> is no client version.</exception>
    public static string SourceOf(string clientVersion)
    {
        // ClientBuild's rule, numbers separated by dots: the version names a resource and is parsed to order the builds.
        if (string.IsNullOrEmpty(clientVersion) || !clientVersion.All(character => char.IsAsciiDigit(character) || character is '.') || !Version.TryParse(clientVersion, out _))
            throw new ArgumentException($"{clientVersion} is no client version.", nameof(clientVersion));

        return _resources.ContainsKey(clientVersion) ? clientVersion : Builds[^1];
    }

    /// <summary>Orders builds oldest first, by their numbers: 3.7.9 before 3.7.13.</summary>
    /// <param name="builds">The builds, each a client version.</param>
    /// <returns>The builds in order.</returns>
    internal static IReadOnlyList<string> Ordered(IEnumerable<string> builds)
    {
        return [.. builds.OrderBy(Version.Parse)];
    }

    /// <summary>Reads the pins of a build: its own when committed, else the latest committed build's, carried over to it.</summary>
    /// <param name="clientVersion">The build mapped.</param>
    /// <returns>Pins that name the build.</returns>
    /// <exception cref="ArgumentException"><paramref name="clientVersion"/> is no client version.</exception>
    public static PinnedPairs For(string clientVersion)
    {
        var source = SourceOf(clientVersion);

        using var stream = typeof(CommittedPins).Assembly.GetManifestResourceStream(_resources[source])!;
        using var reader = new StreamReader(stream);

        var pins = JsonParser.Default.Parse<PinnedPairs>(reader.ReadToEnd());

        pins.ClientVersion = clientVersion;

        return pins;
    }
}
