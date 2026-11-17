using D3B.Bis.Data.Packs;
using D3B.Bis.Protocol.Mapping;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace D3B.Bis.Extraction;

/// <summary>Prepares the files of the installed client's build: its pack, then its schema, then its mapping (D440).</summary>
/// <remarks>
/// <para>
/// It runs in the app's process (D440): whatever a step throws is said, never thrown, so that the app's window goes on and
/// says why.
/// </para>
/// <para>
/// The steps write under <see cref="StagingName"/>, and the three files join the directory the host looks in only once the
/// mapping holds. The host takes each file where it finds it first, this PC's before the app's (D172): a pack and a schema
/// left there beside a mapping the app ships would pair a schema with a mapping made from another.
/// </para>
/// </remarks>
public sealed class ClientPreparation
{
    // Measured on client 3.7.14.18: the pack 58 s, the schema 7.5 s, the mapping 1 s.
    private const double PackShare = 0.87;
    private const double SchemaShare = 0.11;

    /// <summary>The directory, under the host's, that the steps write in until the three files hold together.</summary>
    public const string StagingName = ".preparation";

    private readonly IPreparationSteps _steps;

    /// <summary>Initializes a new instance of the <see cref="ClientPreparation"/> class.</summary>
    /// <param name="steps">What each step does.</param>
    public ClientPreparation(IPreparationSteps steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        _steps = steps;
    }

    /// <summary>Prepares the build's files, on the caller's thread, and says how it went rather than throwing.</summary>
    /// <param name="build">The installed client's build.</param>
    /// <param name="clientDirectory">The client's installation directory.</param>
    /// <param name="root">The directory a build's files are kept in, one directory per build, where the host looks first.</param>
    /// <param name="progress">Told the step running and the share done, or <see langword="null"/>.</param>
    /// <returns>What it gave.</returns>
    public PreparationResult Prepare(string build, string clientDirectory, string root, IProgress<PreparationProgress>? progress = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(build);
        ArgumentException.ThrowIfNullOrEmpty(clientDirectory);
        ArgumentException.ThrowIfNullOrEmpty(root);

        var step = PreparationStep.Pack;
        var staging = Path.Combine(root, StagingName);

        try
        {
            progress?.Report(new PreparationProgress(step, 0));
            _steps.ExtractPack(clientDirectory, staging, new Share(progress, step, 0, PackShare));

            step = PreparationStep.Schema;
            progress?.Report(new PreparationProgress(step, PackShare));
            _steps.WriteSchema(clientDirectory, staging);

            step = PreparationStep.Mapping;
            progress?.Report(new PreparationProgress(step, PackShare + SchemaShare));

            if (!_steps.WriteMapping(build, staging))
                return new PreparationResult(PreparationFailure.ProtocolChanged, step);

            PutInPlace(Path.Combine(staging, build), Path.Combine(root, build));
            progress?.Report(new PreparationProgress(step, 1));

            return new PreparationResult();
        }
        catch (Exception exception)
        {
            // Anything a step throws, Cpp2IL's own faults included, ends the preparation and is said by the banner.
            return new PreparationResult(FailureOf(exception), step, exception.Message);
        }
    }

    private static void PutInPlace(string from, string to)
    {
        Directory.CreateDirectory(to);

        // The mapping last: until it is there, the host does not start on this build, and never on half the files.
        foreach (var name in new[] { PackLocations.FileName, SchemaFile.FileName, MappingFile.FileName })
            File.Move(Path.Combine(from, name), Path.Combine(to, name), true);
    }

    private static PreparationFailure FailureOf(Exception exception)
    {
        return exception switch
        {
            DirectoryNotFoundException => PreparationFailure.NoClient,
            FileNotFoundException or InvalidDataException or DescriptorValidationException or InvalidProtocolBufferException => PreparationFailure.ClientUnread,
            IOException or UnauthorizedAccessException => PreparationFailure.Disk,
            _ => PreparationFailure.Unexpected,
        };
    }

    /// <summary>Turns a step's own share done into the whole preparation's.</summary>
    private sealed class Share : IProgress<double>
    {
        private readonly IProgress<PreparationProgress>? _progress;
        private readonly PreparationStep _step;
        private readonly double _start;
        private readonly double _length;

        public Share(IProgress<PreparationProgress>? progress, PreparationStep step, double start, double length)
        {
            _progress = progress;
            _step = step;
            _start = start;
            _length = length;
        }

        public void Report(double value)
        {
            _progress?.Report(new PreparationProgress(_step, _start + (_length * Math.Clamp(value, 0, 1))));
        }
    }
}
