using Dot2Editor.Core.Mapping;
using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Tests.Mapping;

public class SettingsMapperTests
{
    private static MappingResult MapSingle(string key, DotSettingsValueType type, string value)
    {
        return SettingsMapper.Map([new DotSettingsEntry(key, type, value)]);
    }

    private static EditorConfigProperty MapOne(string key, DotSettingsValueType type, string value)
    {
        var result = MapSingle(key, type, value);
        Assert.Empty(result.Skipped);
        return Assert.Single(result.Properties);
    }

    private static SkippedEntry SkipOne(string key, DotSettingsValueType type, string value)
    {
        var result = MapSingle(key, type, value);
        Assert.Empty(result.Properties);
        return Assert.Single(result.Skipped);
    }

    // ---- Curated mappings ------------------------------------------------

    [Theory]
    [InlineData("INDENT_SIZE", DotSettingsValueType.Int64, "4", "indent_size", "4")]
    [InlineData("TAB_WIDTH", DotSettingsValueType.Int64, "2", "tab_width", "2")]
    [InlineData("WRAP_LIMIT", DotSettingsValueType.Int64, "120", "max_line_length", "120")]
    [InlineData("INDENT_STYLE", DotSettingsValueType.String, "Space", "indent_style", "space")]
    [InlineData("INDENT_STYLE", DotSettingsValueType.String, "Tab", "indent_style", "tab")]
    public void CoreEditorConfigProperties(string option, DotSettingsValueType type, string raw, string name,
        string expected)
    {
        var property = MapOne($"/Default/CodeStyle/CodeFormatting/CSharpFormat/{option}/@EntryValue", type, raw);

        Assert.Equal(new EditorConfigProperty("*.cs", name, expected), property);
    }

    [Theory]
    [InlineData("ForBuiltInTypes", "csharp_style_var_for_built_in_types")]
    [InlineData("ForSimpleTypes", "csharp_style_var_when_type_is_apparent")]
    [InlineData("ForOtherTypes", "csharp_style_var_elsewhere")]
    public void VarUsage_MapsToRoslynProperty(string option, string name)
    {
        var property = MapOne($"/Default/CodeStyle/CSharpVarKeywordUsage/{option}/@EntryValue",
            DotSettingsValueType.String, "UseVar");

        Assert.Equal(new EditorConfigProperty("*.cs", name, "true"), property);
    }

    [Theory]
    [InlineData("UseVar", "true")]
    [InlineData("UseExplicitType", "false")]
    public void VarUsage_Values(string raw, string expected)
    {
        var property = MapOne("/Default/CodeStyle/CSharpVarKeywordUsage/ForBuiltInTypes/@EntryValue",
            DotSettingsValueType.String, raw);

        Assert.Equal(expected, property.Value);
    }

    [Fact]
    public void VarUsage_UseVarWhenEvident_FallsBackInsteadOfBeingDropped()
    {
        // Neither true nor false, so csharp_style_var_* cannot express it, but
        // ReSharper can — the setting must survive rather than being skipped.
        var property = MapOne("/Default/CodeStyle/CSharpVarKeywordUsage/ForBuiltInTypes/@EntryValue",
            DotSettingsValueType.String, "UseVarWhenEvident");

        Assert.Equal(
            new EditorConfigProperty("*.cs", "resharper_csharp_for_built_in_types", "use_var_when_evident"),
            property);
    }

    [Fact]
    public void CuratedValueWithoutStandardEquivalent_FallsBackToResharperProperty()
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_IFELSE/@EntryValue",
            DotSettingsValueType.String, "RequiredForMultilineStatement");

        Assert.Equal(
            new EditorConfigProperty("*.cs", "resharper_csharp_braces_for_ifelse", "required_for_multiline_statement"),
            property);
    }

    [Theory]
    [InlineData("UseKeyword", "true")]
    [InlineData("UseType", "false")]
    public void BuiltInTypeReferenceStyle(string raw, string expected)
    {
        var property = MapOne(
            "/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BUILT_IN_TYPE_REFERENCE_STYLE/@EntryValue",
            DotSettingsValueType.String, raw);

        Assert.Equal(
            new EditorConfigProperty("*.cs", "dotnet_style_predefined_type_for_locals_parameters_members", expected),
            property);
    }

    [Theory]
    [InlineData("False", "false")]
    [InlineData("True", "true")]
    public void SpaceAfterCast_UsesTheNameJetBrainsDocuments(string raw, string expected)
    {
        // The EditorConfig name is "space_after_cast", not a lowercased copy of the
        // .DotSettings key, so the generic fallback would produce a property that
        // neither Rider nor Roslyn recognises.
        var property = MapOne(
            "/Default/CodeStyle/CodeFormatting/CSharpFormat/SPACE_AFTER_TYPECAST_PARENTHESES/@EntryValue",
            DotSettingsValueType.Boolean, raw);

        Assert.Equal(new EditorConfigProperty("*.cs", "csharp_space_after_cast", expected), property);
    }

    [Fact]
    public void XmlFormatterGetsItsOwnSectionSeparateFromXmlDoc()
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/XmlFormatter/INDENT_SIZE/@EntryValue",
            DotSettingsValueType.Int64, "2");

        Assert.Equal("resharper_xml_indent_size", property.Name);
        Assert.Contains("csproj", property.Section);
    }

    [Fact]
    public void ThisQualifier_FlagsExpandIntoOnePropertyPerMemberKind()
    {
        var result = MapSingle(
            "/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/ThisQualifier/INSTANCE_MEMBERS_QUALIFY_MEMBERS/@EntryValue",
            DotSettingsValueType.String, "Field, Property");

        Assert.Empty(result.Skipped);
        Assert.Equal(
        [
            new EditorConfigProperty("*.cs", "dotnet_style_qualification_for_field", "true"),
            new EditorConfigProperty("*.cs", "dotnet_style_qualification_for_property", "true"),
            new EditorConfigProperty("*.cs", "dotnet_style_qualification_for_method", "false"),
            new EditorConfigProperty("*.cs", "dotnet_style_qualification_for_event", "false")
        ], result.Properties);
    }

    [Fact]
    public void ThisQualifier_NoneQualifiesNothing()
    {
        var result = MapSingle(
            "/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/ThisQualifier/INSTANCE_MEMBERS_QUALIFY_MEMBERS/@EntryValue",
            DotSettingsValueType.String, "None");

        Assert.All(result.Properties, p => Assert.Equal("false", p.Value));
    }

    [Theory]
    [InlineData("Required", "true")]
    [InlineData("NotRequired", "false")]
    [InlineData("NotRequiredForBoth", "false")]
    [InlineData("RequiredForMultiline", "when_multiline")]
    public void BracesForIfElse_MapsToPreferBraces(string raw, string expected)
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_IFELSE/@EntryValue",
            DotSettingsValueType.String, raw);

        Assert.Equal(new EditorConfigProperty("*.cs", "csharp_prefer_braces", expected), property);
    }

    // ---- Inspection severities -------------------------------------------

    [Theory]
    [InlineData("ERROR", "error")]
    [InlineData("WARNING", "warning")]
    [InlineData("SUGGESTION", "suggestion")]
    [InlineData("HINT", "hint")]
    [InlineData("DO_NOT_SHOW", "do_not_show")]
    public void InspectionSeverity_Values(string raw, string expected)
    {
        var property = MapOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=ArrangeThisQualifier/@EntryIndexedValue",
            DotSettingsValueType.String, raw);

        Assert.Equal(
            new EditorConfigProperty("*", "resharper_arrange_this_qualifier_highlighting", expected),
            property);
    }

    [Fact]
    public void InspectionSeverity_EscapedIdIsDecoded()
    {
        var property = MapOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=UnusedMember_002EGlobal/@EntryIndexedValue",
            DotSettingsValueType.String, "SUGGESTION");

        Assert.Equal("resharper_unused_member_global_highlighting", property.Name);
    }

    [Theory]
    [InlineData("/Default/CodeInspection/Highlighting/IncludeWarningsInSwea/@EntryValue")]
    [InlineData("/Default/CodeInspection/Highlighting/ValueAnalysisMode/@EntryValue")]
    public void InspectionSeverity_NonSeverityEntriesInTheSameGroupAreSkipped(string key)
    {
        // Seen in real Rider projects; these sit next to InspectionSeverities but
        // are solution-wide analysis switches, not formatting settings.
        var skipped = SkipOne(key, DotSettingsValueType.String, "DEFAULT");

        Assert.Contains("solution-wide analysis", skipped.Reason);
    }

    [Theory]
    [InlineData("ERROR", "error")]
    [InlineData("WARNING", "warning")]
    [InlineData("SUGGESTION", "suggestion")]
    [InlineData("HINT", "silent")]
    [InlineData("DO_NOT_SHOW", "none")]
    public void CompilerWarning_BecomesARoslynDiagnosticSeverity(string raw, string expected)
    {
        // "_003A" is ":", so the id decodes to "CSharpWarnings::CS0109". Emitting it as a
        // resharper_* property would produce a name containing "::", which is not valid.
        var property = MapOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=CSharpWarnings_003A_003ACS0109/@EntryIndexedValue",
            DotSettingsValueType.String, raw);

        Assert.Equal(
            new EditorConfigProperty("*.cs", "dotnet_diagnostic.CS0109.severity", expected),
            property);
    }

    [Fact]
    public void CompilerWarning_VisualBasicGoesIntoTheVisualBasicSection()
    {
        var property = MapOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=VBWarnings_003A_003ABC40008/@EntryIndexedValue",
            DotSettingsValueType.String, "DO_NOT_SHOW");

        Assert.Equal(new EditorConfigProperty("*.vb", "dotnet_diagnostic.BC40008.severity", "none"), property);
    }

    [Fact]
    public void RemovedEntryTombstoneIsReportedAsSuch()
    {
        // ReSharper writes "@EntryIndexRemoved = True" when a setting is deleted. Reading
        // that "True" as a severity produced a misleading "unknown severity" report.
        var skipped = SkipOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=ArrangeRedundantParentheses/@EntryIndexRemoved",
            DotSettingsValueType.Boolean, "True");

        Assert.Contains("tombstone", skipped.Reason);
    }

    [Fact]
    public void SeverityWithAnEmptyValueIsReportedAsEmptyNotUnknown()
    {
        var skipped = SkipOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=InvertIf/@EntryIndexedValue",
            DotSettingsValueType.String, "");

        Assert.Contains("no value", skipped.Reason);
    }

    [Theory]
    [InlineData("CodeStyle/CodeFormatting/CppCodeStyle/BRACES_FOR_IFELSE", "resharper_cpp_braces_for_ifelse")]
    [InlineData("CodeStyle/TypeScriptCodeStyle/NoImplicitAny", "resharper_js_no_implicit_any")]
    [InlineData("CodeStyle/RazorCodeStyle/PreferQualifiedReference", "resharper_razor_prefer_qualified_reference")]
    public void NewlyCoveredLanguageGroups(string path, string expectedName)
    {
        var property = MapOne($"/Default/{path}/@EntryValue", DotSettingsValueType.Boolean, "True");

        Assert.Equal(expectedName, property.Name);
    }

    [Fact]
    public void InspectionSeverity_IdThatCannotBeNamedIsSkippedNotEmitted()
    {
        var skipped = SkipOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=Weird_003A_003AThing/@EntryIndexedValue",
            DotSettingsValueType.String, "WARNING");

        Assert.Contains("no valid EditorConfig property name", skipped.Reason);
    }

    [Fact]
    public void InspectionSeverity_UnknownValueIsSkipped()
    {
        var skipped = SkipOne(
            "/Default/CodeInspection/Highlighting/InspectionSeverities/=BadIndent/@EntryIndexedValue",
            DotSettingsValueType.String, "MAYBE");

        Assert.Contains("severity", skipped.Reason);
    }

    // ---- resharper_* fallback --------------------------------------------

    [Fact]
    public void Fallback_CSharpFormatOptionBecomesResharperProperty()
    {
        var property = MapOne(
            "/Default/CodeStyle/CodeFormatting/CSharpFormat/ANONYMOUS_METHOD_DECLARATION_BRACES/@EntryValue",
            DotSettingsValueType.String, "NEXT_LINE");

        Assert.Equal(
            new EditorConfigProperty("*.cs", "resharper_csharp_anonymous_method_declaration_braces", "next_line"),
            property);
    }

    [Fact]
    public void Fallback_BooleanIsLowercased()
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/CSharpFormat/ALIGN_LINQ_QUERY/@EntryValue",
            DotSettingsValueType.Boolean, "True");

        Assert.Equal("true", property.Value);
    }

    [Fact]
    public void Fallback_PascalCaseValueBecomesSnakeCase()
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/XmlDocFormatter/IndentSubtags/@EntryValue",
            DotSettingsValueType.String, "ZeroIndent");

        Assert.Equal("zero_indent", property.Value);
    }

    [Fact]
    public void Fallback_NestedPathUsesLastSegmentOnly()
    {
        var property = MapOne(
            "/Default/CodeStyle/CodeFormatting/CSharpCodeStyle/ThisQualifier/INSTANCE_MEMBERS_QUALIFY_DECLARED_IN/@EntryValue",
            DotSettingsValueType.String, "0");

        Assert.Equal("resharper_csharp_instance_members_qualify_declared_in", property.Name);
    }

    [Theory]
    [InlineData("CodeStyle/CodeFormatting/XmlDocFormatter/INDENT_SIZE", "*.cs", "resharper_xmldoc_indent_size")]
    [InlineData("CodeStyle/CodeFormatting/VBFormat/INDENT_SIZE", "*.vb", "resharper_vb_indent_size")]
    [InlineData("CodeStyle/CodeFormatting/HtmlFormatter/INDENT_SIZE", "*.{html,htm}", "resharper_html_indent_size")]
    [InlineData("CodeStyle/CodeFormatting/CssFormatter/INDENT_SIZE", "*.css", "resharper_css_indent_size")]
    public void Fallback_EachLanguageKeepsItsOwnSectionAndPrefix(string path, string section, string name)
    {
        var property = MapOne($"/Default/{path}/@EntryValue", DotSettingsValueType.Int64, "2");

        Assert.Equal(new EditorConfigProperty(section, name, "2"), property);
    }

    [Fact]
    public void Fallback_CppUsesItsOwnSection()
    {
        var property = MapOne("/Default/CodeStyle/CodeFormatting/CppFormatting/NAMESPACE_INDENTATION/@EntryValue",
            DotSettingsValueType.String, "None");

        Assert.Equal("resharper_cpp_namespace_indentation", property.Name);
        Assert.Contains("cpp", property.Section);
    }

    // ---- Skips ------------------------------------------------------------

    [Theory]
    [InlineData("/Default/CodeStyle/Naming/CSharpNaming/PredefinedNamingRules/=Parameters/@EntryIndexedValue",
        "naming")]
    [InlineData("/Default/UserDictionary/Words/=foo/@EntryIndexedValue", "dictionary")]
    [InlineData("/Default/Environment/Editor/ShowLineNumbers/@EntryValue", "environment")]
    public void SkippedGroups_ReportTheirReason(string key, string reasonFragment)
    {
        var skipped = SkipOne(key, DotSettingsValueType.String, "x");

        Assert.Contains(reasonFragment, skipped.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownGroup_IsSkipped()
    {
        var skipped = SkipOne("/Default/SomeFuturePlugin/Options/Enabled/@EntryValue", DotSettingsValueType.Boolean,
            "True");

        Assert.Contains("no known mapping", skipped.Reason);
    }

    [Theory]
    [InlineData("/NotDefault/Whatever/@EntryValue")]
    [InlineData("/Default/CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE")]
    public void MalformedKey_IsSkippedNotThrown(string key)
    {
        var skipped = SkipOne(key, DotSettingsValueType.String, "x");

        Assert.Contains("expected", skipped.Reason);
    }

    [Fact]
    public void IndexedEntryInsideKnownGroup_IsSkipped()
    {
        var skipped = SkipOne("/Default/CodeStyle/CodeFormatting/CSharpFormat/CustomRules/=Foo/@EntryIndexedValue",
            DotSettingsValueType.String, "bar");

        Assert.Contains("indexed", skipped.Reason);
    }
}
