using System.Security.Cryptography;
using System.Text;
using D3B.Bis.Data.Releases;

namespace D3B.Bis.Data.Tests.Releases;

public sealed class ClientReleaseDownloaderTests : IDisposable
{
    private const string Tag = "client-3.7.14.18";

    private static readonly ClientBuild _build = new("3.7.14.18", null);
    private static readonly string[] _names = ["data-12.pack", "schema-1.binpb", "mapping-17.binpb"];

    private readonly FakeReleaseServer _server = new();
    private readonly TemporaryDirectory _directory = new();

    private static Dictionary<string, byte[]> Files => new(StringComparer.Ordinal)
    {
        ["data-12.pack"] = Encoding.ASCII.GetBytes("the pack of the build, longer than the others"),
        ["schema-1.binpb"] = Encoding.ASCII.GetBytes("the schema"),
        ["mapping-17.binpb"] = Encoding.ASCII.GetBytes("the mapping"),
    };

    private string Target => _directory.Combine(_build.Version);

    [Fact]
    public async Task APublishedBuildsFilesArePutInItsDirectoryOnceEachHoldsItsChecksum()
    {
        _server.Publish(Tag, Files);

        var seen = new List<ClientDownloadProgress>();

        await Downloader().DownloadAsync(_build, _names, Target, new Progress(seen.Add), TestContext.Current.CancellationToken);

        foreach (var (name, bytes) in Files)
            Assert.Equal(bytes, await File.ReadAllBytesAsync(Path.Combine(Target, name), TestContext.Current.CancellationToken));

        Assert.Equal(_names.Order(StringComparer.Ordinal), Directory.GetFiles(Target).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Equal(new ClientDownloadProgress(66, 66), seen[^1]);
        Assert.All(seen, progress => Assert.Equal(66, progress.Total));
    }

    [Fact]
    public async Task TheReleaseIsAskedOfGitHubsApiAndItsAssetsAsBytes()
    {
        _server.Publish(Tag, Files);

        await Downloader(token: "the-token").DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken);

        var first = _server.Requests[0];

        Assert.Equal(new Uri(FakeReleaseServer.Api, $"repos/Owner/Repo/releases/tags/{Tag}"), first.RequestUri);
        Assert.All(_server.Requests, request => Assert.Equal("Bearer the-token", request.Headers.Authorization?.ToString()));
        Assert.All(_server.Requests, request => Assert.NotEmpty(request.Headers.UserAgent));
        Assert.All(_server.Requests.Skip(1), request => Assert.Contains(request.Headers.Accept, accept => accept.MediaType == "application/octet-stream"));
    }

    [Fact]
    public async Task ABuildWithNoReleaseIsSaidNotPublishedAndNothingIsWritten()
    {
        _server.Publish("client-3.7.13.17", Files);

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.NotPublished, exception.Failure);
        Assert.Contains(Tag, exception.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Target));
    }

    [Fact]
    public async Task AReleaseWithoutTheFormatsReadIsSaidToLackThemAndNothingIsWritten()
    {
        var files = Files;

        files.Remove("data-12.pack");
        files["data-13.pack"] = Encoding.ASCII.GetBytes("a pack of a newer format");
        _server.Publish(Tag, files);

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.MissingFiles, exception.Failure);
        Assert.Contains("data-12.pack", exception.Message, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Target));
    }

    [Fact]
    public async Task AFileWithoutItsChecksumIsMissingToo()
    {
        _server.PublishAssets(Tag, Files);

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.MissingFiles, exception.Failure);
        Assert.Contains("data-12.pack.sha256", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFileThatDoesNotHoldItsChecksumIsRefusedAndNoneIsPutInPlace()
    {
        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (name, bytes) in Files)
        {
            assets[name] = name == "mapping-17.binpb" ? [.. bytes, (byte)'!'] : bytes;
            assets[ClientRelease.ChecksumNameOf(name)] = Encoding.ASCII.GetBytes(ClientRelease.FormatChecksum(SHA256.HashData(bytes), name));
        }

        _server.PublishAssets(Tag, assets);

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.Checksum, exception.Failure);
        Assert.Contains("mapping-17.binpb", exception.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.Exists(Target) ? Directory.GetFiles(Target) : []);
    }

    [Fact]
    public async Task ANetworkCutDuringADownloadIsSaidAndLeavesTheFilesAlreadyThereAsTheyWere()
    {
        _server.Publish(Tag, Files);
        _server.Cut("data-12.pack", 10);
        Directory.CreateDirectory(Target);
        await File.WriteAllTextAsync(Path.Combine(Target, "schema-1.binpb"), "an older schema", TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.Network, exception.Failure);
        Assert.IsType<IOException>(exception.InnerException);
        Assert.Equal(["schema-1.binpb"], Directory.GetFiles(Target).Select(Path.GetFileName));
        Assert.Equal("an older schema", await File.ReadAllTextAsync(Path.Combine(Target, "schema-1.binpb"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GitHubOutOfReachIsANetworkFailure()
    {
        _server.IsDown = true;

        var exception = await Assert.ThrowsAsync<ClientReleaseException>(() => Downloader().DownloadAsync(_build, _names, Target, null, TestContext.Current.CancellationToken));

        Assert.Equal(ClientReleaseFailure.Network, exception.Failure);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task ACancelledDownloadIsNoFailureOfTheRelease()
    {
        _server.Publish(Tag, Files);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Downloader().DownloadAsync(_build, _names, Target, null, new CancellationToken(true)));
    }

    public void Dispose()
    {
        _server.Dispose();
        _directory.Dispose();
    }

    private ClientReleaseDownloader Downloader(string? token = null)
    {
        return new ClientReleaseDownloader(new HttpClient(_server, false), _server.Repository, token, FakeReleaseServer.Api);
    }

    private sealed class Progress : IProgress<ClientDownloadProgress>
    {
        private readonly Action<ClientDownloadProgress> _report;

        public Progress(Action<ClientDownloadProgress> report)
        {
            _report = report;
        }

        public void Report(ClientDownloadProgress value)
        {
            _report(value);
        }
    }
}
