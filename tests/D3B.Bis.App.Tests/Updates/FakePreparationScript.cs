namespace D3B.Bis.App.Tests.Updates;

/// <summary>What a test's preparation does, shared by every <see cref="FakePreparation"/> the app makes, and what they were asked.</summary>
internal sealed class FakePreparationScript
{
    public const string Build = "3.7.14.18";

    public List<string> Calls { get; } = [];

    public SemaphoreSlim? Gate { get; init; }

    public Exception? PackThrows { get; set; }

    public Exception? SchemaThrows { get; set; }

    public bool MappingHolds { get; set; } = true;
}
