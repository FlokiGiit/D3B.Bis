namespace D3B.Bis.Extraction;

/// <summary>What each step of preparing a build's files does: the installed client's, or a test's double (D440).</summary>
public interface IPreparationSteps
{
    /// <summary>Extracts the build's pack from the client into its directory under a root.</summary>
    /// <param name="clientDirectory">The client's installation directory.</param>
    /// <param name="root">The directory a build's files are kept in, one directory per build.</param>
    /// <param name="progress">Told the share of the step done, from 0 to 1.</param>
    void ExtractPack(string clientDirectory, string root, IProgress<double> progress);

    /// <summary>Reads the client's schema into its build's directory under a root.</summary>
    /// <param name="clientDirectory">The client's installation directory.</param>
    /// <param name="root">The directory a build's files are kept in, one directory per build.</param>
    void WriteSchema(string clientDirectory, string root);

    /// <summary>Makes the build's mapping from its schema under a root, and writes it when it holds.</summary>
    /// <param name="build">The client build mapped.</param>
    /// <param name="root">The directory a build's files are kept in, one directory per build.</param>
    /// <returns>Whether the mapping holds and was written.</returns>
    bool WriteMapping(string build, string root);
}
