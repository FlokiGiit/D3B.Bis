using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using D3B.Bis.Data;
using D3B.Bis.Protocol.Mapping;
using D3B.Bis.Protocol.Readable;
using D3B.Bis.Protocol.Translation;
using Google.Protobuf;

namespace D3B.Bis.Extraction.Mapping;

/// <summary>Matches D3B's readable protocol with a build's schema, audits the result and writes the mapping only when it holds (D11, D43, D44), for the tools and the app (D440).</summary>
public static class MappingMaker
{
    /// <summary>Makes the mapping of a build and writes it into its directory under a root when it holds.</summary>
    /// <param name="build">The client build mapped.</param>
    /// <param name="root">The directory mappings are kept in, one directory per client version, beside the schemas.</param>
    /// <param name="schema">The build's schema.</param>
    /// <param name="pins">The build's pinned pairs, or <see langword="null"/>.</param>
    /// <param name="anchors">What the captures of the build measured, or none.</param>
    /// <param name="payloads">The messages the captures carried in an <c>Any</c>, or none.</param>
    /// <param name="log">Where the matching and the audit are said, line by line.</param>
    /// <param name="capturesHold">Whether the captures measured nothing that contradicts the plan of anchors.</param>
    /// <param name="json">Whether to say the mapping as JSON too, to read it.</param>
    /// <returns>The mapping, and where it was written once it holds.</returns>
    /// <exception cref="IOException">The mapping could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The mapping's directory refused the write.</exception>
    public static MappingOutcome Make(ClientBuild build, string root, ProtocolSchema schema, PinnedPairs? pins, IReadOnlyList<Anchor> anchors, IReadOnlyList<CapturedPayload> payloads, TextWriter log, bool capturesHold = true, bool json = false)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(anchors);
        ArgumentNullException.ThrowIfNull(payloads);
        ArgumentNullException.ThrowIfNull(log);

        var stopwatch = Stopwatch.StartNew();
        var failed = !capturesHold;
        var matcher = new ProtocolMatcher(ReadableProtocol.Files, schema, ReadableProtocol.Scopes);
        var result = matcher.Match(pins, anchors);

        log.WriteLine($"Matching: {result.Messages.Count} messages paired, {Evidence(result.Messages.Select(match => match.Evidence))}; {result.Enums.Count} enums, {Evidence(result.Enums.Select(match => match.Evidence))}");

        foreach (var match in result.Messages.Where(match => match.Score is not null))
            log.WriteLine($"  scored: {match.Readable.FullName} is .{match.Obfuscated.FullName}, {match.Score:F2}");

        foreach (var match in result.Enums.Where(match => match.Score is not null))
            log.WriteLine($"  scored: {match.Readable.FullName} is .{match.Obfuscated.FullName}, {match.Score:F2}");

        foreach (var proposal in PinProposals.For(result, schema, payloads))
            log.WriteLine($"  unpaired: {proposal}");

        foreach (var conflict in result.Conflicts)
            log.WriteLine($"  conflict: {conflict}");

        failed |= result.Conflicts.Count > 0;

        var tool = typeof(ProtocolMatcher).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var mapping = result.ToMapping(build.Version, schema.Sha256, ReadableProtocol.Digest, tool);
        var audit = ContractAudit.Audit(ReadableProtocol.Files, schema, mapping);

        log.WriteLine($"Audit: {audit.Messages} messages, {audit.Fields} fields, {audit.Enums} enums and {audit.Values} values checked; {audit.Failures.Count} failures, {audit.Notes.Count} notes, {audit.Unread.Count} partial messages");

        foreach (var failure in audit.Failures)
            log.WriteLine($"  failure: {failure}");

        foreach (var note in audit.Notes)
            log.WriteLine($"  note: {note}");

        foreach (var line in audit.Unread)
            log.WriteLine($"  partial: {line}");

        // The bot builds what the client sends, whole: a message the captures show the client sending cannot be partial.
        var carriedByClient = payloads.Where(payload => payload.FromClient > 0).Select(payload => payload.Message).ToHashSet();

        foreach (var match in result.Messages.Where(match => carriedByClient.Contains(match.Obfuscated) && match.Readable.GetOptions()?.GetExtension(OptionsExtensions.Partial) is true))
        {
            log.WriteLine($"  failure: {match.Readable.FullName} is partial, and the captures show the client sending it.");
            failed = true;
        }

        // The host refuses these too (D47): a mapping it would not start on is not written.
        var renumbered = ProtocolTranslation.Renumbered(mapping);

        log.WriteLine($"Numbers: {(renumbered.Count is 0 ? "every readable field and value has its own number on the wire" : $"{renumbered.Count} renumbered, which D3B does not translate")}");

        foreach (var line in renumbered)
            log.WriteLine($"  renumbered: {line}");

        failed |= renumbered.Count > 0;

        if (json)
            log.WriteLine(new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation()).Format(mapping));

        if (failed || !audit.Passed)
            return new MappingOutcome(mapping, null);

        var path = MappingFile.PathOf(root, build.Version);
        var bytes = MappingFile.Write(path, mapping);

        log.WriteLine($"Mapping: {path}, {bytes.Length:N0} bytes, format {MappingFile.FormatVersion}, in {stopwatch.Elapsed.TotalSeconds:F1} s");
        log.WriteLine($"SHA-256: {Convert.ToHexStringLower(SHA256.HashData(bytes))}");

        return new MappingOutcome(mapping, path);
    }

    private static string Evidence(IEnumerable<MatchEvidence> evidence)
    {
        var counts = evidence.GroupBy(kind => kind).OrderBy(group => group.Key).Select(group => $"{group.Count()} by {Name(group.Key)}").ToList();

        return counts.Count is 0 ? "none" : string.Join(", ", counts);
    }

    private static string Name(MatchEvidence evidence)
    {
        return evidence switch
        {
            MatchEvidence.Pin => "pin",
            MatchEvidence.Capture => "capture",
            MatchEvidence.Field => "field",
            MatchEvidence.Score => "score",
            _ => "nothing",
        };
    }
}
