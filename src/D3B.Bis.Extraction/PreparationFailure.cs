namespace D3B.Bis.Extraction;

/// <summary>Why preparing a build's files failed (D440).</summary>
public enum PreparationFailure
{
    /// <summary>The mapping does not hold: the game's protocol changed, and the readable protocol or its pins must follow it.</summary>
    ProtocolChanged,

    /// <summary>The client's data or code does not read as the extraction expects: the extraction must follow it.</summary>
    ClientUnread,

    /// <summary>No client is installed where the host looks.</summary>
    NoClient,

    /// <summary>A file could not be read or written.</summary>
    Disk,

    /// <summary>Something the steps do not foresee was thrown.</summary>
    Unexpected,
}
