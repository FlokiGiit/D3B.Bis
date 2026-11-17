namespace D3B.Bis.Extraction;

/// <summary>Where preparing a build's files stands (D440).</summary>
/// <param name="Step">The step running.</param>
/// <param name="Done">The share of the whole preparation done, from 0 to 1.</param>
public readonly record struct PreparationProgress(PreparationStep Step, double Done);
