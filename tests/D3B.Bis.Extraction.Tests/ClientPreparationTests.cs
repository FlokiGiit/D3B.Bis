using D3B.Bis.Data.Packs;
using D3B.Bis.Protocol.Mapping;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace D3B.Bis.Extraction.Tests;

public sealed class ClientPreparationTests : IDisposable
{
    private const string Build = "3.7.14.18";
    private const string Client = @"C:\Ankama\Dofus-dofus3";

    private static readonly string[] _files = [PackLocations.FileName, SchemaFile.FileName, MappingFile.FileName];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "D3B.Bis.Extraction.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ThePackTheSchemaThenTheMappingAreMadeFromTheClientAndLandWhereTheHostLooks()
    {
        var steps = new FakeSteps { Build = Build };

        var result = new ClientPreparation(steps).Prepare(Build, Client, _root);

        Assert.True(result.IsReady);
        Assert.Equal([$"pack {Client}", $"schema {Client}", $"mapping {Build}"], steps.Calls);
        Assert.All(_files, name => Assert.Equal($"the {name} of {Build}", File.ReadAllText(Path.Combine(_root, Build, name))));
    }

    [Fact]
    public void NothingLandsWhereTheHostLooksWhenTheMappingDoesNotHold()
    {
        // The host takes each file where it finds it first (D172): a pack and a schema of this PC's beside an app's mapping
        // would pair a schema with a mapping made from another.
        var result = new ClientPreparation(new FakeSteps { Build = Build, MappingHolds = false }).Prepare(Build, Client, _root);

        Assert.False(result.IsReady);
        Assert.All(_files, name => Assert.False(File.Exists(Path.Combine(_root, Build, name)), name));
    }

    [Fact]
    public void ThreeFilesMadeAgainReplaceThoseInPlace()
    {
        Directory.CreateDirectory(Path.Combine(_root, Build));

        foreach (var name in _files)
            File.WriteAllText(Path.Combine(_root, Build, name), "an older one");

        new ClientPreparation(new FakeSteps { Build = Build }).Prepare(Build, Client, _root);

        Assert.All(_files, name => Assert.Equal($"the {name} of {Build}", File.ReadAllText(Path.Combine(_root, Build, name))));
    }

    [Fact]
    public void TheProgressNamesEachStepAndGrowsToTheWhole()
    {
        var steps = new FakeSteps { Build = Build, PackProgress = [0, 0.5, 1] };
        var seen = new List<PreparationProgress>();

        new ClientPreparation(steps).Prepare(Build, Client, _root, new Recorder(seen));

        Assert.Equal([PreparationStep.Pack, PreparationStep.Schema, PreparationStep.Mapping], seen.Select(progress => progress.Step).Distinct());
        Assert.Equal(0, seen[0].Done);
        Assert.Equal(1, seen[^1].Done);
        Assert.All(seen.Zip(seen.Skip(1)), pair => Assert.True(pair.First.Done <= pair.Second.Done, $"{pair.First} then {pair.Second}"));

        // The pack takes 58 s of the 67 s measured on 3.7.14.18: half of it is past a third of the whole.
        var pack = seen.Where(progress => progress.Step is PreparationStep.Pack).Select(progress => progress.Done).Distinct().ToList();

        Assert.Equal(3, pack.Count);
        Assert.InRange(pack[1], 0.35, 0.5);
    }

    [Fact]
    public void AMappingThatDoesNotHoldIsSaidAsTheProtocolChanged()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, MappingHolds = false }).Prepare(Build, Client, _root);

        Assert.False(result.IsReady);
        Assert.Equal(PreparationFailure.ProtocolChanged, result.Failure);
        Assert.Equal(PreparationStep.Mapping, result.Step);
    }

    [Fact]
    public void AClientTheExtractionMisreadsStopsBeforeTheNextStep()
    {
        var steps = new FakeSteps { Build = Build, PackThrows = new InvalidDataException("The maps' bundle holds no MapData.") };

        var result = new ClientPreparation(steps).Prepare(Build, Client, _root);

        Assert.Equal(new PreparationResult(PreparationFailure.ClientUnread, PreparationStep.Pack, "The maps' bundle holds no MapData."), result);
        Assert.Equal([$"pack {Client}"], steps.Calls);
    }

    [Fact]
    public void ACodeThatReadsAsNoSchemaIsAClientMisread()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, SchemaThrows = DeclaredTwice() }).Prepare(Build, Client, _root);

        Assert.Equal(PreparationFailure.ClientUnread, result.Failure);
        Assert.Equal(PreparationStep.Schema, result.Step);
    }

    [Fact]
    public void AFileTheClientNoLongerHoldsIsAClientMisreadNotTheDisks()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, PackThrows = new FileNotFoundException("The client has no Breeds table.") }).Prepare(Build, Client, _root);

        Assert.Equal(new PreparationResult(PreparationFailure.ClientUnread, PreparationStep.Pack, "The client has no Breeds table."), result);
    }

    [Fact]
    public void ADiskThatRefusesAFileIsSaid()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, SchemaThrows = new UnauthorizedAccessException("Access to the path is denied.") }).Prepare(Build, Client, _root);

        Assert.Equal(new PreparationResult(PreparationFailure.Disk, PreparationStep.Schema, "Access to the path is denied."), result);
    }

    [Fact]
    public void AClientGoneIsSaid()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, PackThrows = new DirectoryNotFoundException("No client in C:\\Ankama.") }).Prepare(Build, Client, _root);

        Assert.Equal(PreparationFailure.NoClient, result.Failure);
    }

    [Fact]
    public void WhatTheStepsDoNotForeseeIsSaidRatherThanThrown()
    {
        var result = new ClientPreparation(new FakeSteps { Build = Build, MappingThrows = new InvalidOperationException("Cpp2IL lost its state.") }).Prepare(Build, Client, _root);

        Assert.Equal(new PreparationResult(PreparationFailure.Unexpected, PreparationStep.Mapping, "Cpp2IL lost its state."), result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private static DescriptorValidationException DeclaredTwice()
    {
        var file = new FileDescriptorProto { Name = "twice.proto", Package = "twice" };

        file.MessageType.Add(new DescriptorProto { Name = "Same" });
        file.MessageType.Add(new DescriptorProto { Name = "Same" });

        return Assert.Throws<DescriptorValidationException>(() => FileDescriptor.BuildFromByteStrings([file.ToByteString()]));
    }

    /// <summary>Records on the thread that reports, unlike <see cref="Progress{T}"/>, which would post to a context.</summary>
    private sealed class Recorder : IProgress<PreparationProgress>
    {
        private readonly List<PreparationProgress> _seen;

        public Recorder(List<PreparationProgress> seen)
        {
            _seen = seen;
        }

        public void Report(PreparationProgress value)
        {
            _seen.Add(value);
        }
    }
}
