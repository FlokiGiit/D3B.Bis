namespace D3B.Bis.App.Updates;

/// <summary>Where the download of the installed client's files stands (D440).</summary>
internal enum ClientFilesStatus
{
    /// <summary>Not asked for.</summary>
    Idle,

    /// <summary>Takes the files from the build's release.</summary>
    Downloading,

    /// <summary>The files are in place: the app reads them once it restarts.</summary>
    Ready,

    /// <summary>The last download failed: <see cref="ClientFiles.Failure"/> and <see cref="ClientFiles.FailureMessage"/> say why.</summary>
    Failed,
}
