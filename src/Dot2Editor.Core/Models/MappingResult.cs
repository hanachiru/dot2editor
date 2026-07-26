namespace Dot2Editor.Core.Models;

/// <summary>
///     The outcome of mapping .DotSettings entries onto EditorConfig properties.
/// </summary>
/// <param name="Properties">The properties to write.</param>
/// <param name="Skipped">Entries that produced no properties, each with a reason.</param>
/// <param name="Warnings">Entries that converted only partially.</param>
public sealed record MappingResult(
    IReadOnlyList<EditorConfigProperty> Properties,
    IReadOnlyList<SkippedEntry> Skipped,
    IReadOnlyList<ConversionWarning> Warnings);
