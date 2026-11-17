using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using D3B.Bis.Data.Releases;

namespace D3B.Bis.Data.Tests.Releases;

/// <summary>GitHub's releases as a test sets them: a release by its tag, its assets, and an asset whose download is cut.</summary>
/// <remarks>It answers the two requests GitHub's REST API answers for a release: the release by its tag, and an asset's bytes.</remarks>
internal sealed class FakeReleaseServer : HttpMessageHandler
{
    public static readonly Uri Api = new("https://api.test/");

    private readonly Dictionary<string, Dictionary<string, byte[]>> _releases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _cuts = new(StringComparer.Ordinal);
    private readonly List<string> _assets = [];

    public string Repository { get; } = "Owner/Repo";

    public List<HttpRequestMessage> Requests { get; } = [];

    public bool IsDown { get; set; }

    /// <summary>Publishes a release whose assets are each file with its checksum, as D24 writes them.</summary>
    public void Publish(string tag, IReadOnlyDictionary<string, byte[]> files)
    {
        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (name, bytes) in files)
        {
            assets[name] = bytes;
            assets[ClientRelease.ChecksumNameOf(name)] = Encoding.ASCII.GetBytes(ClientRelease.FormatChecksum(SHA256.HashData(bytes), name));
        }

        PublishAssets(tag, assets);
    }

    /// <summary>Publishes a release holding exactly these assets.</summary>
    public void PublishAssets(string tag, IReadOnlyDictionary<string, byte[]> assets)
    {
        _releases[tag] = new Dictionary<string, byte[]>(assets, StringComparer.Ordinal);
    }

    /// <summary>Has the download of an asset end, as a network cut ends it, once that many bytes are sent.</summary>
    public void Cut(string name, long after)
    {
        _cuts[name] = after;
    }

    private static (string Tag, string Name) Split(string key)
    {
        var slash = key.IndexOf('/', StringComparison.Ordinal);

        return (key[..slash], key[(slash + 1)..]);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (IsDown)
            throw new HttpRequestException("No such host is known. (api.test:443)");

        var path = request.RequestUri!.AbsolutePath;
        var release = $"/repos/{Repository}/releases/tags/";
        var asset = $"/repos/{Repository}/releases/assets/";

        if (path.StartsWith(release, StringComparison.Ordinal))
            return Task.FromResult(Release(path[release.Length..]));

        if (path.StartsWith(asset, StringComparison.Ordinal))
            return Task.FromResult(Asset(int.Parse(path[asset.Length..], CultureInfo.InvariantCulture), request));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private HttpResponseMessage Release(string tag)
    {
        if (!_releases.TryGetValue(tag, out var assets))
            return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"message":"Not Found"}""") };

        var listed = assets.Select(pair =>
        {
            var id = IdOf(tag, pair.Key);

            return new Dictionary<string, object>
            {
                ["id"] = id,
                ["name"] = pair.Key,
                ["size"] = pair.Value.LongLength,
                ["url"] = new Uri(Api, $"repos/{Repository}/releases/assets/{id}").ToString(),
                ["browser_download_url"] = $"https://github.test/{Repository}/releases/download/{tag}/{pair.Key}",
            };
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new Dictionary<string, object> { ["tag_name"] = tag, ["assets"] = listed }), Encoding.UTF8, "application/json"),
        };
    }

    private HttpResponseMessage Asset(int id, HttpRequestMessage request)
    {
        if (id < 0 || id >= _assets.Count)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        // GitHub answers an asset's description unless its bytes are asked for.
        if (!request.Headers.Accept.Any(accept => accept.MediaType == "application/octet-stream"))
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };

        var (tag, name) = Split(_assets[id]);
        var bytes = _releases[tag][name];
        Stream body = _cuts.TryGetValue(name, out var after) ? new CutStream(bytes, after) : new MemoryStream(bytes);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) };
    }

    private int IdOf(string tag, string name)
    {
        var key = $"{tag}/{name}";
        var id = _assets.IndexOf(key);

        if (id >= 0)
            return id;

        _assets.Add(key);

        return _assets.Count - 1;
    }

    /// <summary>A body that ends with an <see cref="IOException"/> once some of it was read, as a connection reset ends it.</summary>
    private sealed class CutStream : Stream
    {
        private readonly MemoryStream _bytes;
        private readonly long _after;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => _bytes.Position;
            set => throw new NotSupportedException();
        }

        public CutStream(byte[] bytes, long after)
        {
            _bytes = new MemoryStream(bytes);
            _after = after;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytes.Position >= _after)
                throw new IOException("Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host.");

            return _bytes.Read(buffer, offset, (int)Math.Min(count, _after - _bytes.Position));
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _bytes.Dispose();

            base.Dispose(disposing);
        }
    }
}
