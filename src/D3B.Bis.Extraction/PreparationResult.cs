namespace D3B.Bis.Extraction;

/// <summary>What preparing a build's files gave (D440).</summary>
/// <param name="Failure">Why it failed, or <see langword="null"/> once the three files are in place.</param>
/// <param name="Step">The step that failed, or <see langword="null"/>.</param>
/// <param name="Message">What the step failed on, or <see langword="null"/>.</param>
public sealed record PreparationResult(PreparationFailure? Failure = null, PreparationStep? Step = null, string? Message = null)
{
    /// <summary>Gets whether the pack, the schema and the mapping are in place.</summary>
    public bool IsReady => Failure is null;
}
