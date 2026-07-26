namespace Dot2Editor.Core.Models;

/// <summary>
///     A .DotSettings entry that could not be converted, with the reason why.
/// </summary>
public sealed record SkippedEntry(string Key, string Reason);
