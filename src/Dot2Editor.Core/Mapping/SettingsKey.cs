namespace Dot2Editor.Core.Mapping;

/// <summary>
/// A .DotSettings key split into the parts mapping cares about. Everything this tool
/// knows about the shape of a key lives here.
/// </summary>
/// <param name="Path">
/// The settings path between "/Default/" and the trailing "@..." part, e.g.
/// "CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE".
/// </param>
internal readonly record struct SettingsKey(string Path)
{
    private const string DefaultPrefix = "/Default/";

    /// <summary>ReSharper's marker for a setting that was deleted rather than changed.</summary>
    private const string RemovedSuffix = "/@EntryIndexRemoved";

    /// <summary>A key that records a removed setting carries no value to convert.</summary>
    internal static bool IsTombstone(string rawKey) =>
        rawKey.EndsWith(RemovedSuffix, StringComparison.Ordinal);

    /// <summary>
    /// Splits "/Default/A/B/C/@EntryValue" into "A/B/C". Fails for any other shape,
    /// including keys with no "@..." part at all.
    /// </summary>
    internal static bool TryParse(string rawKey, out SettingsKey key)
    {
        key = default;
        if (!rawKey.StartsWith(DefaultPrefix, StringComparison.Ordinal)) return false;

        var lastSlash = rawKey.LastIndexOf('/');
        if (lastSlash < DefaultPrefix.Length || lastSlash + 1 >= rawKey.Length || rawKey[lastSlash + 1] != '@')
            return false;

        key = new SettingsKey(rawKey[DefaultPrefix.Length..lastSlash]);
        return true;
    }

    internal bool StartsWith(string prefix) => Path.StartsWith(prefix, StringComparison.Ordinal);

    /// <summary>The part of the path after a group prefix, e.g. "INDENT_SIZE".</summary>
    internal string After(string prefix) => Path[prefix.Length..];
}
