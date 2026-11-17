namespace D3B.Bis.Data.Releases;

/// <summary>A client build's files could not be taken from its release: <see cref="Failure"/> says why.</summary>
public sealed class ClientReleaseException : Exception
{
    /// <summary>Gets why the files could not be taken.</summary>
    public ClientReleaseFailure Failure { get; }

    /// <summary>Initializes a new instance of the <see cref="ClientReleaseException"/> class.</summary>
    public ClientReleaseException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ClientReleaseException"/> class.</summary>
    /// <param name="message">What failed.</param>
    public ClientReleaseException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ClientReleaseException"/> class.</summary>
    /// <param name="message">What failed.</param>
    /// <param name="innerException">What the failure came from.</param>
    public ClientReleaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ClientReleaseException"/> class.</summary>
    /// <param name="failure">Why the files could not be taken.</param>
    /// <param name="message">What failed.</param>
    /// <param name="innerException">What the failure came from, if anything.</param>
    public ClientReleaseException(ClientReleaseFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }
}
