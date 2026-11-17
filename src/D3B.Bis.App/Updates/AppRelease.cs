using System.Reflection;

namespace D3B.Bis.App.Updates;

/// <summary>What this build of the app is: its version. It ships no client's files (D441).</summary>
internal static class AppRelease
{
    /// <summary>Gets the app's version, as its tag names it, without the commit.</summary>
    public static string Version { get; } = ReadVersion();

    private static string ReadVersion()
    {
        var version = typeof(AppRelease).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        var metadata = version.IndexOf('+', StringComparison.Ordinal);

        return metadata < 0 ? version : version[..metadata];
    }
}
