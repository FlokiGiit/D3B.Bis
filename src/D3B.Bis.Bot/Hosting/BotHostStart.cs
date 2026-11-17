using Microsoft.Extensions.Hosting;

namespace D3B.Bis.Bot.Hosting;

/// <summary>What starting the bots' host gave: the host, or why it did not start.</summary>
/// <param name="Host">The host, started, which the caller stops and disposes; <see langword="null"/> when it did not start.</param>
/// <param name="Failure">Why the host did not start, and what to do about it; <see langword="null"/> when it started.</param>
/// <param name="MissingBuild">
/// The installed client's build when the host did not start for lack of its pack, schema or mapping, which a newer version
/// of the app ships (D172); <see langword="null"/> otherwise.
/// </param>
/// <param name="DataDirectory">
/// Where a build's files go on this machine, looked in first, once <paramref name="MissingBuild"/> is set: the app puts the
/// files it prepares there (D440); <see langword="null"/> otherwise.
/// </param>
/// <param name="ClientDirectory">
/// The installed client's directory, once <paramref name="MissingBuild"/> is set: the app prepares the files from it (D440);
/// <see langword="null"/> otherwise.
/// </param>
public sealed record BotHostStart(IHost? Host, string? Failure, string? MissingBuild = null, string? DataDirectory = null, string? ClientDirectory = null);
