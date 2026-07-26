namespace Dot2Editor.Core.Models;

/// <summary>
///     The outcome of merging generated properties into an existing .editorconfig.
/// </summary>
/// <param name="EditorConfigText">The merged file content.</param>
/// <param name="Added">Properties that were not present before.</param>
/// <param name="Updated">Properties that existed with a different value.</param>
/// <param name="Unchanged">Properties that already had the generated value.</param>
public sealed record MergeResult(string EditorConfigText, int Added, int Updated, int Unchanged);
