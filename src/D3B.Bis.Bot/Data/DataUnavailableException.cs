namespace D3B.Bis.Bot.Data;

/// <summary>The exception thrown when the game data of the installed client cannot be had: no client, or no pack of its build.</summary>
/// <remarks>Its message says what is missing and how to get it; the host shows it and does not start.</remarks>
public sealed class DataUnavailableException : Exception
{
    /// <summary>Gets the installed client's build when no pack of it is found, rather than one found and refused.</summary>
    /// <remarks>The app tells such a build apart, which a newer version of D3B ships (D172).</remarks>
    public string? MissingBuild { get; init; }

    /// <summary>Gets where the build's files go on this machine, once <see cref="MissingBuild"/> is set: where the app puts its release's files (D440).</summary>
    public string? DataDirectory { get; init; }

    /// <summary>Initializes a new instance of the <see cref="DataUnavailableException"/> class.</summary>
    public DataUnavailableException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataUnavailableException"/> class.</summary>
    /// <param name="message">What is missing, and how to get it.</param>
    public DataUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataUnavailableException"/> class.</summary>
    /// <param name="message">What is missing, and how to get it.</param>
    /// <param name="innerException">The error met while looking for it.</param>
    public DataUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
