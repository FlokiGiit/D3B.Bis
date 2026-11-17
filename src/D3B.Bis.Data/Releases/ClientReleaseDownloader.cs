using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace D3B.Bis.Data.Releases;

/// <summary>Takes a client build's files from its release, each checked against the SHA-256 published beside it (D24).</summary>
/// <remarks>
/// <para>
/// The release is asked of GitHub's REST API by its tag, and each asset's bytes through the API too: the one way that reads a
/// public repository with no token and a private one with a token. GitHub answers an asset with a redirect to its storage,
/// which <see cref="HttpClient"/> follows without the token, as it drops <c>Authorization</c> on every redirect.
/// </para>
/// <para>
/// A file is written beside its place under a temporary name, and every file is moved into place only once all of them hold
/// their checksums: a release that fails leaves the directory as it was.
/// </para>
/// <para>
/// The app's update of its client files and <c>scripts/embed.cs</c> share this code (D440).
/// </para>
/// </remarks>
public sealed class ClientReleaseDownloader
{
    private const string PartSuffix = ".download";
    private const int BufferSize = 81920;

    private static readonly ProductInfoHeaderValue _userAgent = new("D3B.Bis", "1");

    private readonly HttpClient _http;
    private readonly string _repository;
    private readonly string? _token;
    private readonly Uri _api;

    /// <summary>Gets the root of GitHub's REST API.</summary>
    public static Uri GitHubApi { get; } = new("https://api.github.com/");

    /// <summary>Initializes a new instance of the <see cref="ClientReleaseDownloader"/> class.</summary>
    /// <param name="http">The client that asks GitHub.</param>
    /// <param name="repository">The repository whose releases ship the files, as owner/name.</param>
    /// <param name="token">A GitHub token, which a private repository's releases need, or null.</param>
    /// <param name="api">The root of GitHub's REST API, <see cref="GitHubApi"/> unless a test's.</param>
    /// <exception cref="ArgumentException"><paramref name="repository"/> does not name a repository as owner/name.</exception>
    public ClientReleaseDownloader(HttpClient http, string repository, string? token = null, Uri? api = null)
    {
        ArgumentNullException.ThrowIfNull(http);

        if (!ClientRelease.IsRepository(repository))
            throw new ArgumentException($"'{repository}' does not name a GitHub repository as owner/name.", nameof(repository));

        _http = http;
        _repository = repository;
        _token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        _api = api ?? GitHubApi;
    }

    /// <summary>Downloads files of a build's release into a directory, once each holds the SHA-256 published beside it.</summary>
    /// <param name="build">The client build, whose release is tagged <c>client-&lt;version&gt;</c>.</param>
    /// <param name="names">The files' names, as the release's assets are named.</param>
    /// <param name="directory">Where they go, made if missing.</param>
    /// <param name="progress">Told how much is downloaded, or null.</param>
    /// <param name="cancellationToken">Stops the download.</param>
    /// <returns>A task that completes once every file is in place.</returns>
    /// <exception cref="ClientReleaseException">The release is missing, lacks a file, a file does not hold its checksum, or the network failed.</exception>
    /// <exception cref="IOException">A file could not be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The directory cannot be written.</exception>
    public async Task DownloadAsync(ClientBuild build, IReadOnlyList<string> names, string directory, IProgress<ClientDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(names);
        ArgumentException.ThrowIfNullOrEmpty(directory);

        var tag = ClientRelease.TagOf(build);
        var assets = await AssetsAsync(tag, cancellationToken).ConfigureAwait(false);
        var missing = names.SelectMany(name => new[] { name, ClientRelease.ChecksumNameOf(name) }).Where(name => !assets.ContainsKey(name)).ToList();

        if (missing.Count > 0)
            throw new ClientReleaseException(ClientReleaseFailure.MissingFiles, $"Release {tag} of {_repository} lacks {string.Join(", ", missing)}.");

        var expected = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in names)
            expected[name] = await ChecksumAsync(tag, name, assets[ClientRelease.ChecksumNameOf(name)].Url, cancellationToken).ConfigureAwait(false);

        var total = names.Sum(name => assets[name].Size);
        var parts = names.Select(name => Path.Combine(directory, name + PartSuffix)).ToList();
        var done = 0L;

        Directory.CreateDirectory(directory);

        try
        {
            for (var index = 0; index < names.Count; index++)
            {
                var name = names[index];
                var hash = await DownloadAsync(tag, name, assets[name].Url, parts[index], downloaded => progress?.Report(new ClientDownloadProgress(done + downloaded, total)), cancellationToken).ConfigureAwait(false);

                if (hash != expected[name])
                    throw new ClientReleaseException(ClientReleaseFailure.Checksum, $"{name} of release {tag} has SHA-256 {hash}, not {expected[name]} as published.");

                done += new FileInfo(parts[index]).Length;
            }

            for (var index = 0; index < names.Count; index++)
                File.Move(parts[index], Path.Combine(directory, names[index]), true);
        }
        finally
        {
            foreach (var part in parts)
                File.Delete(part);
        }
    }

    private static ClientReleaseException Network(string what, Exception exception)
    {
        return new ClientReleaseException(ClientReleaseFailure.Network, $"{what}: {exception.Message}", exception);
    }

    private async Task<Dictionary<string, Asset>> AssetsAsync(string tag, CancellationToken cancellationToken)
    {
        using var request = Request(new Uri(_api, $"repos/{_repository}/releases/tags/{Uri.EscapeDataString(tag)}"), "application/vnd.github+json");
        using var response = await SendAsync(request, $"release {tag} of {_repository} cannot be read", cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound)
            throw new ClientReleaseException(ClientReleaseFailure.NotPublished, $"{_repository} has no release {tag}.");

        Ensure(response, $"release {tag} of {_repository}");

        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            await using (stream.ConfigureAwait(false))
            {
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var assets = new Dictionary<string, Asset>(StringComparer.Ordinal);

                foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
                    assets[asset.GetProperty("name").GetString()!] = new Asset(new Uri(asset.GetProperty("url").GetString()!), asset.GetProperty("size").GetInt64());

                return assets;
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or UriFormatException)
        {
            throw new ClientReleaseException(ClientReleaseFailure.Network, $"GitHub described release {tag} of {_repository} in a form this code does not read.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw Network($"release {tag} of {_repository} cannot be read", exception);
        }
    }

    private async Task<string> ChecksumAsync(string tag, string name, Uri url, CancellationToken cancellationToken)
    {
        var checksum = ClientRelease.ChecksumNameOf(name);

        using var request = Request(url, "application/octet-stream");
        using var response = await SendAsync(request, $"{checksum} of release {tag} cannot be downloaded", cancellationToken).ConfigureAwait(false);

        Ensure(response, $"{checksum} of release {tag}");

        string text;

        try
        {
            text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw Network($"{checksum} of release {tag} cannot be downloaded", exception);
        }

        try
        {
            return ClientRelease.ParseChecksum(text, name);
        }
        catch (InvalidDataException exception)
        {
            throw new ClientReleaseException(ClientReleaseFailure.Checksum, $"{checksum} of release {tag} does not read: {exception.Message}", exception);
        }
    }

    private async Task<string> DownloadAsync(string tag, string name, Uri url, string path, Action<long> progress, CancellationToken cancellationToken)
    {
        var what = $"{name} of release {tag} cannot be downloaded";

        using var request = Request(url, "application/octet-stream");
        using var response = await SendAsync(request, what, cancellationToken).ConfigureAwait(false);

        Ensure(response, $"{name} of release {tag}");

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];
        var downloaded = 0L;
        Stream body;

        try
        {
            body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            throw Network(what, exception);
        }

        await using (body.ConfigureAwait(false))
        {
            var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);

            await using (file.ConfigureAwait(false))
            {
                while (true)
                {
                    int read;

                    // A read that fails is the network's; a write that fails is the disk's, and is not said as a release's failure.
                    try
                    {
                        read = await body.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (exception is HttpRequestException or IOException)
                    {
                        throw Network(what, exception);
                    }

                    if (read is 0)
                        break;

                    hash.AppendData(buffer, 0, read);
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    downloaded += read;
                    progress(downloaded);
                }
            }
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string what, CancellationToken cancellationToken)
    {
        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw Network(what, exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            // The client's own timeout, not the caller's cancellation.
            throw Network(what, exception);
        }
    }

    private void Ensure(HttpResponseMessage response, string what)
    {
        if (!response.IsSuccessStatusCode)
            throw new ClientReleaseException(ClientReleaseFailure.Network, $"GitHub answered {(int)response.StatusCode} {response.ReasonPhrase} for {what} of {_repository}.");
    }

    private HttpRequestMessage Request(Uri url, string accept)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // GitHub's API refuses a request without a User-Agent.
        request.Headers.UserAgent.Add(_userAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        if (_token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        return request;
    }

    private sealed record Asset(Uri Url, long Size);
}
