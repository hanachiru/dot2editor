using Dot2Editor.Core.Generation;
using Dot2Editor.Core.Mapping;
using Dot2Editor.Core.Models;
using Dot2Editor.Core.Parsing;

namespace Dot2Editor.Core;

/// <summary>
///     Entry point of the core library: converts .DotSettings XML text into
///     .editorconfig text. Pure string-in / string-out — no file system or OS
///     dependencies, so it can run in a browser (Blazor WebAssembly) as-is.
/// </summary>
public static class DotSettingsConverter
{
    /// <exception cref="DotSettingsParseException">The input is not a valid .DotSettings document.</exception>
    public static ConversionResult Convert(string dotSettingsXml, ConvertOptions? options = null)
    {
        options ??= new ConvertOptions();
        var entries = DotSettingsParser.Parse(dotSettingsXml);
        var mapped = SettingsMapper.Map(entries);
        var text = EditorConfigGenerator.Generate(mapped.Properties, options);
        return new ConversionResult(text, mapped.Properties, mapped.Skipped, mapped.Warnings);
    }
}
