using D3B.Bis.Protocol.Mapping;

namespace D3B.Bis.Extraction.Mapping;

/// <summary>What making a build's mapping gave.</summary>
/// <param name="Mapping">The mapping made, written or not.</param>
/// <param name="Path">Where the mapping was written, or <see langword="null"/> when it does not hold and nothing was written.</param>
public sealed record MappingOutcome(ProtocolMapping Mapping, string? Path)
{
    /// <summary>Gets whether the mapping holds against the build's schema and the readable protocol, and was written.</summary>
    public bool Holds => Path is not null;
}
