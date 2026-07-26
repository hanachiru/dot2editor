namespace Dot2Editor.Core.Mapping;

/// <summary>
/// Maps a .DotSettings settings group to the .editorconfig section its options belong to,
/// and to the prefix ReSharper/Rider uses for that language. ReSharper reads its own
/// options from EditorConfig as "resharper_&lt;language&gt;_&lt;option name in lower case&gt;".
///
/// Only groups whose EditorConfig prefix could be confirmed are listed. A group that is
/// missing here is reported as skipped rather than converted into an invented property.
/// </summary>
/// <param name="GroupPath">The settings path this profile owns, ending in "/".</param>
/// <param name="Section">The .editorconfig section its options belong in.</param>
/// <param name="PropertyPrefix">The prefix ReSharper reads those options under.</param>
internal sealed record LanguageProfile(string GroupPath, string Section, string PropertyPrefix)
{
    /// <summary>Checked in order; the first group path the key starts with wins.</summary>
    internal static readonly IReadOnlyList<LanguageProfile> All =
    [
        new("CodeStyle/CodeFormatting/CSharpFormat/", Sections.CSharp, "resharper_csharp_"),
        new("CodeStyle/CodeFormatting/CSharpCodeStyle/", Sections.CSharp, "resharper_csharp_"),
        new("CodeStyle/CSharpVarKeywordUsage/", Sections.CSharp, "resharper_csharp_"),
        new("CodeStyle/CSharpUsing/", Sections.CSharp, "resharper_csharp_"),
        // XML doc comments live inside C# files, so they share the [*.cs] section.
        new("CodeStyle/CodeFormatting/XmlDocFormatter/", Sections.CSharp, "resharper_xmldoc_"),
        new("CodeStyle/CodeFormatting/VBFormat/", Sections.VisualBasic, "resharper_vb_"),
        new("CodeStyle/CodeFormatting/CppFormatting/", Sections.Cpp, "resharper_cpp_"),
        new("CodeStyle/CodeFormatting/CppCodeStyle/", Sections.Cpp, "resharper_cpp_"),
        new("CodeStyle/CodeFormatting/HtmlFormatter/", Sections.Html, "resharper_html_"),
        new("CodeStyle/CodeFormatting/CssFormatter/", Sections.Css, "resharper_css_"),
        new("CodeStyle/CodeFormatting/XmlFormatter/", Sections.Xml, "resharper_xml_"),
        new("CodeStyle/CodeFormatting/JavaScriptCodeFormatting/", Sections.JavaScript, "resharper_js_"),
        // JetBrains documents TypeScript options under the JavaScript prefix.
        new("CodeStyle/TypeScriptCodeStyle/", Sections.TypeScript, "resharper_js_"),
        new("CodeStyle/RazorCodeStyle/", Sections.Razor, "resharper_razor_"),
        // Unity shaders
        new("CodeStyle/CodeFormatting/ShaderLabFormat/", Sections.ShaderLab, "resharper_shaderlab_"),
        new("CodeStyle/CodeFormatting/ProtobufCodeFormatting/", Sections.Protobuf, "resharper_protobuf_"),
        new("CodeStyle/CodeFormatting/ResxFormatter/", Sections.Resx, "resharper_resx_"),
        // Generalized options apply to every language at once, so they take no language
        // qualifier and belong in the [*] section.
        new("CodeStyle/CodeFormatting/CommonFormatter/", Sections.Any, "resharper_")
    ];

    internal static LanguageProfile? Find(string path) =>
        All.FirstOrDefault(profile => path.StartsWith(profile.GroupPath, StringComparison.Ordinal));
}
