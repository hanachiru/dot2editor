using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Tests;

/// <summary>
///     Golden-file test over the comprehensive sample. Any change in mapping,
///     section routing or ordering shows up here as a readable diff.
/// </summary>
public class ComprehensiveConversionTests
{
    private static readonly ConversionResult Result = DotSettingsConverter.Convert(TestDataFiles.Comprehensive);

    [Fact]
    public void MatchesGoldenFile()
    {
        var expected = TestDataFiles.Read("Comprehensive.editorconfig").ReplaceLineEndings("\n");

        Assert.Equal(expected, Result.EditorConfigText);
    }

    [Fact]
    public void SectionsAreOrderedWithStarFirst()
    {
        var sections = Result.EditorConfigText
            .Split('\n')
            .Where(line => line.StartsWith('['))
            .ToArray();

        Assert.Equal(
        [
            "[*]",
            "[*.cs]",
            "[*.css]",
            "[*.proto]",
            "[*.resx]",
            "[*.shader]",
            "[*.vb]",
            "[*.{c,c++,cc,cpp,cxx,h,h++,hh,hpp,hxx}]",
            "[*.{html,htm}]",
            "[*.{js,jsx}]",
            "[*.{razor,cshtml}]",
            "[*.{ts,tsx}]",
            "[*.{xml,xsd,xsl,xslt,config,csproj,props,targets,nuspec,ruleset}]"
        ], sections);
    }

    [Fact]
    public void XmlDocIndentSizeDoesNotCollideWithCSharpIndentSize()
    {
        Assert.Contains("\nindent_size = 4\n", Result.EditorConfigText);
        Assert.Contains("\nresharper_xmldoc_indent_size = 2\n", Result.EditorConfigText);
    }

    [Fact]
    public void InspectionSeveritiesLandInTheAnySection()
    {
        Assert.All(
            Result.Properties.Where(p => p.Name.EndsWith("_highlighting", StringComparison.Ordinal)),
            p => Assert.Equal("*", p.Section));
    }

    [Fact]
    public void EveryPropertyNameIsLowerSnakeCase()
    {
        // Naming conventions use dotted names such as "dotnet_naming_rule.<id>.severity".
        // Diagnostic ids keep their own casing, because they are identifiers Roslyn matches
        // literally: dotnet_diagnostic.CS0109.severity.
        Assert.All(Result.Properties, p =>
        {
            if (p.Name.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal))
            {
                Assert.Matches(@"^dotnet_diagnostic\.[A-Za-z0-9]+\.severity$", p.Name);
                return;
            }

            Assert.Matches("^[a-z0-9_]+(\\.[a-z0-9_]+)*$", p.Name);
        });
    }

    [Fact]
    public void EveryNamingRuleHasItsSymbolsStyleAndSeverity()
    {
        var ruleIds = Result.Properties
            .Where(p => p.Name.StartsWith("dotnet_naming_rule.", StringComparison.Ordinal))
            .Select(p => p.Name.Split('.')[1])
            .Distinct()
            .ToArray();

        Assert.NotEmpty(ruleIds);
        foreach (var id in ruleIds)
        {
            // All three rule properties are required for a rule to take effect, and both
            // referenced entities must exist.
            Assert.Contains(Result.Properties, p => p.Name == $"dotnet_naming_rule.{id}.severity");
            Assert.Contains(Result.Properties, p => p.Name == $"dotnet_naming_rule.{id}.symbols");
            Assert.Contains(Result.Properties, p => p.Name == $"dotnet_naming_rule.{id}.style");
            Assert.Contains(Result.Properties, p => p.Name == $"dotnet_naming_symbols.{id}_symbols.applicable_kinds");
            Assert.Contains(Result.Properties,
                p => p.Name == $"dotnet_naming_symbols.{id}_symbols.applicable_accessibilities");
            Assert.Contains(Result.Properties, p => p.Name == $"dotnet_naming_style.{id}_style.capitalization");
        }
    }

    [Fact]
    public void NamingRulesUseOnlyValuesRoslynUnderstands()
    {
        string[] kinds =
        [
            "*", "namespace", "class", "struct", "interface", "enum", "property", "method",
            "field", "event", "delegate", "parameter", "type_parameter", "local", "local_function"
        ];
        string[] accessibilities =
        [
            "*", "public", "internal", "private", "protected", "protected_internal", "private_protected", "local"
        ];
        string[] modifiers = ["abstract", "async", "const", "readonly", "static"];
        string[] capitalizations = ["pascal_case", "camel_case", "first_word_upper", "all_upper", "all_lower"];
        string[] severities = ["none", "silent", "suggestion", "warning", "error"];

        foreach (var property in Result.Properties)
        {
            var allowed = property.Name switch
            {
                var n when n.EndsWith(".applicable_kinds", StringComparison.Ordinal) => kinds,
                var n when n.EndsWith(".applicable_accessibilities", StringComparison.Ordinal) => accessibilities,
                var n when n.EndsWith(".required_modifiers", StringComparison.Ordinal) => modifiers,
                var n when n.EndsWith(".capitalization", StringComparison.Ordinal) => capitalizations,
                var n when n.StartsWith("dotnet_naming_rule.", StringComparison.Ordinal)
                           && n.EndsWith(".severity", StringComparison.Ordinal) => severities,
                _ => null
            };

            if (allowed is null) continue;

            foreach (var value in property.Value.Split(',', StringSplitOptions.TrimEntries))
                Assert.Contains(value, allowed);
        }
    }

    [Fact]
    public void PropertyValuesAreTrimmedAndSingleLine()
    {
        Assert.All(Result.Properties, p =>
        {
            Assert.Equal(p.Value.Trim(), p.Value);
            Assert.DoesNotContain('\n', p.Value);
        });
    }

    [Fact]
    public void AnExplicitlyEmptySettingKeepsItsEmptyValue()
    {
        // Real .DotSettings files set options to an empty string on purpose — an empty
        // list is not the same as an unset option, so the empty value has to survive.
        var property = Assert.Single(
            Result.Properties, p => p.Name == "resharper_html_tags_are_not_indented_inside");

        Assert.Equal(string.Empty, property.Value);
    }
}
