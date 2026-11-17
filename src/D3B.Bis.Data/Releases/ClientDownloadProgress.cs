namespace D3B.Bis.Data.Releases;

/// <summary>How much of a client build's files is downloaded.</summary>
/// <param name="Downloaded">The bytes downloaded so far.</param>
/// <param name="Total">The bytes of every file, as the release lists them.</param>
public readonly record struct ClientDownloadProgress(long Downloaded, long Total);
