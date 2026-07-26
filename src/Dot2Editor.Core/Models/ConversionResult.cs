namespace Dot2Editor.Core.Models;

/// <summary>
///     The outcome of a .DotSettings → .editorconfig conversion.
/// </summary>
/// <param name="EditorConfigText">The generated .editorconfig content.</param>
/// <param name="Properties">Every property that made it into the output.</param>
/// <param name="Skipped">Entries that had no usable mapping, each with a reason.</param>
/// <param name="Warnings">Entries that converted, but not in full.</param>
public sealed record ConversionResult(
    string EditorConfigText,
    IReadOnlyList<EditorConfigProperty> Properties,
    IReadOnlyList<SkippedEntry> Skipped,
    IReadOnlyList<ConversionWarning> Warnings);
