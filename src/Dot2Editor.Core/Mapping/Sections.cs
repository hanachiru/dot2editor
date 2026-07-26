namespace Dot2Editor.Core.Mapping;

/// <summary>
/// The .editorconfig section globs this tool writes into, in one place so that a language
/// is spelled the same way wherever it is referenced.
/// </summary>
internal static class Sections
{
    /// <summary>Applies to every file.</summary>
    internal const string Any = "*";

    internal const string CSharp = "*.cs";
    internal const string VisualBasic = "*.vb";
    internal const string Cpp = "*.{c,c++,cc,cpp,cxx,h,h++,hh,hpp,hxx}";
    internal const string Html = "*.{html,htm}";
    internal const string Css = "*.css";
    internal const string JavaScript = "*.{js,jsx}";
    internal const string TypeScript = "*.{ts,tsx}";
    internal const string Razor = "*.{razor,cshtml}";
    internal const string ShaderLab = "*.shader";
    internal const string Protobuf = "*.proto";
    internal const string Resx = "*.resx";
    internal const string Xml = "*.{xml,xsd,xsl,xslt,config,csproj,props,targets,nuspec,ruleset}";
}
