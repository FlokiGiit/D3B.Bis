namespace D3B.Bis.Extraction;

/// <summary>A step of preparing a build's files, in the order they run (D440).</summary>
public enum PreparationStep
{
    /// <summary>The pack, extracted from the client's data.</summary>
    Pack,

    /// <summary>The obfuscated schema, read from the client's code.</summary>
    Schema,

    /// <summary>The mapping, matched from the readable protocol, the schema and the pins carried over.</summary>
    Mapping,
}
