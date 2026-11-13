namespace D3B.Bis.Data.Packs;

/// <summary>The name a pack stores for the type of an entry.</summary>
/// <remarks>
/// The name is part of the format: a pack written before the namespaces took the <c>D3B.Bis</c> root names its types
/// under <c>D3B</c>, and a read asks for the name the pack holds. A type keeps that name whatever root its namespace
/// has, so that those packs are read and the ones written now are the same bytes.
/// </remarks>
internal static class PackTypeNames
{
    private const string Root = "D3B.Bis.";

    private const string StoredRoot = "D3B.";

    /// <summary>Gets the name a pack stores for a type.</summary>
    /// <typeparam name="T">The type an entry is written or read as.</typeparam>
    /// <returns>The type's full name, its root as the format has it.</returns>
    public static string Of<T>()
    {
        return Stored<T>.Name;
    }

    private static string StoredName(Type type)
    {
        var name = type.FullName!;

        return name.StartsWith(Root, StringComparison.Ordinal) ? string.Concat(StoredRoot, name.AsSpan(Root.Length)) : name;
    }

    private static class Stored<T>
    {
        public static readonly string Name = StoredName(typeof(T));
    }
}
