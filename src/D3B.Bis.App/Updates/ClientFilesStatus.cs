namespace D3B.Bis.App.Updates;

/// <summary>Where the preparation of the installed client's files stands (D440).</summary>
internal enum ClientFilesStatus
{
    /// <summary>Not asked for.</summary>
    Idle,

    /// <summary>Prepares the pack, the schema and the mapping from the installed client.</summary>
    Preparing,

    /// <summary>The files are in place: the app reads them once it restarts.</summary>
    Ready,

    /// <summary>The last preparation failed: <see cref="ClientFiles.Result"/> says why.</summary>
    Failed,
}
