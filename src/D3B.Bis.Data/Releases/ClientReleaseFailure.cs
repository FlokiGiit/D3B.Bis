namespace D3B.Bis.Data.Releases;

/// <summary>Why a client build's files could not be taken from its release.</summary>
public enum ClientReleaseFailure
{
    /// <summary>The repository has no release for the build: it is not published yet.</summary>
    NotPublished,

    /// <summary>The release lacks a file of the formats this code reads, or its checksum.</summary>
    MissingFiles,

    /// <summary>A file downloaded does not hold the SHA-256 its release publishes, or its checksum does not read.</summary>
    Checksum,

    /// <summary>GitHub could not be reached, answered an error, or a download was cut.</summary>
    Network,
}
