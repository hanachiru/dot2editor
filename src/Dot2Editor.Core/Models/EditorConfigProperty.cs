namespace Dot2Editor.Core.Models;

/// <summary>
///     A single property destined for the generated .editorconfig.
/// </summary>
/// <param name="Section">
///     The section glob without brackets, e.g. "*.cs". An empty string means the preamble (before any
///     section).
/// </param>
/// <param name="Name">The property name, e.g. "indent_size".</param>
/// <param name="Value">The property value, e.g. "4".</param>
public sealed record EditorConfigProperty(string Section, string Name, string Value);
