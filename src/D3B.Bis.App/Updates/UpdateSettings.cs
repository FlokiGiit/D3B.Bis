using D3B.Bis.Data.Releases;
using Microsoft.Extensions.Configuration;

namespace D3B.Bis.App.Updates;

/// <summary>Where the app looks for its newer versions (D173) and for the installed client's files (D440), as its command line or environment says.</summary>
internal static class UpdateSettings
{
    public const string SourceKey = "Updates:Source";
    public const string ClientRepositoryKey = "Updates:ClientRepository";

    /// <summary>The repository whose releases ship the clients' files the app downloads: D3B's, which publishes them in the formats this code reads (D439, D440).</summary>
    public const string DefaultClientRepository = "AerafalDev/D3B";

    /// <summary>Gets the repository whose releases ship the app.</summary>
    public static string DefaultSource => $"https://github.com/{ClientRelease.DefaultRepository}";

    /// <summary>Gets the source the configuration names, such as a folder of releases packed on this machine, else the repository.</summary>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The source.</returns>
    public static string SourceIn(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration[SourceKey] is { Length: > 0 } source ? source.Trim() : DefaultSource;
    }

    /// <summary>Gets the repository whose releases ship the installed client's files, as owner/name: the one the configuration names, else D3B's.</summary>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The repository.</returns>
    public static string ClientRepositoryIn(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // The name ends up in a URL: one that is no repository falls back rather than stopping the app as it starts.
        return configuration[ClientRepositoryKey]?.Trim() is { } repository && ClientRelease.IsRepository(repository) ? repository : DefaultClientRepository;
    }
}
