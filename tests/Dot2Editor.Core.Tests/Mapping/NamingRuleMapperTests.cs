using Dot2Editor.Core.Mapping;
using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Tests.Mapping;

public class NamingRuleMapperTests
{
    private const string CSharpNaming = "/Default/CodeStyle/Naming/CSharpNaming/";

    private static MappingResult MapPolicies(params (string Key, string Policy)[] rules)
    {
        return SettingsMapper.Map(rules.Select(r =>
            new DotSettingsEntry(CSharpNaming + r.Key + "/@EntryIndexedValue", DotSettingsValueType.String, r.Policy)));
    }

    private static string ValueOf(MappingResult result, string name)
    {
        return Assert.Single(result.Properties, p => p.Name == name).Value;
    }

    // ---- Predefined rules -------------------------------------------------

    [Fact]
    public void PredefinedRule_ProducesRuleSymbolsAndStyle()
    {
        var result = MapPolicies(("PredefinedNamingRules/=Parameters",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" />"""));

        Assert.Empty(result.Skipped);
        Assert.Equal("warning", ValueOf(result, "dotnet_naming_rule.parameters.severity"));
        Assert.Equal("parameters_symbols", ValueOf(result, "dotnet_naming_rule.parameters.symbols"));
        Assert.Equal("parameters_style", ValueOf(result, "dotnet_naming_rule.parameters.style"));
        Assert.Equal("parameter", ValueOf(result, "dotnet_naming_symbols.parameters_symbols.applicable_kinds"));
        Assert.Equal("*", ValueOf(result, "dotnet_naming_symbols.parameters_symbols.applicable_accessibilities"));
        Assert.Equal("camel_case", ValueOf(result, "dotnet_naming_style.parameters_style.capitalization"));
        Assert.All(result.Properties, p => Assert.Equal("*.cs", p.Section));
    }

    [Fact]
    public void PredefinedRule_PrefixBecomesRequiredPrefix()
    {
        var result = MapPolicies(("PredefinedNamingRules/=PrivateInstanceFields",
            """<Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" />"""));

        Assert.Equal("_", ValueOf(result, "dotnet_naming_style.private_instance_fields_style.required_prefix"));
        Assert.Equal("field",
            ValueOf(result, "dotnet_naming_symbols.private_instance_fields_symbols.applicable_kinds"));
        Assert.Equal("private",
            ValueOf(result, "dotnet_naming_symbols.private_instance_fields_symbols.applicable_accessibilities"));
    }

    [Fact]
    public void PredefinedRule_InspectFalseBecomesSeverityNone()
    {
        var result = MapPolicies(("PredefinedNamingRules/=Parameters",
            """<Policy Inspect="False" Prefix="" Suffix="" Style="aaBb" />"""));

        Assert.Equal("none", ValueOf(result, "dotnet_naming_rule.parameters.severity"));
    }

    [Fact]
    public void PredefinedRule_RequiredModifiersComeFromTheRuleName()
    {
        var result = MapPolicies(("PredefinedNamingRules/=PrivateStaticReadonly",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" />"""));

        Assert.Equal("static, readonly",
            ValueOf(result, "dotnet_naming_symbols.private_static_readonly_symbols.required_modifiers"));
    }

    [Fact]
    public void PredefinedRule_UnknownNameIsSkipped()
    {
        var result = MapPolicies(("PredefinedNamingRules/=SomethingNew",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" />"""));

        Assert.Empty(result.Properties);
        Assert.Contains("unrecognised", Assert.Single(result.Skipped).Reason);
    }

    // ---- Styles -----------------------------------------------------------

    [Theory]
    [InlineData("AaBb", "pascal_case", null)]
    [InlineData("aaBb", "camel_case", null)]
    [InlineData("AA_BB", "all_upper", "_")]
    [InlineData("aa_bb", "all_lower", "_")]
    [InlineData("Aa_bb", "first_word_upper", "_")]
    [InlineData("AaBb_AaBb", "pascal_case", "_")]
    public void Styles_MapToCapitalizationAndWordSeparator(string style, string capitalization, string? separator)
    {
        var result = MapPolicies(("PredefinedNamingRules/=Parameters",
            $"""<Policy Inspect="True" Prefix="" Suffix="" Style="{style}" />"""));

        Assert.Equal(capitalization, ValueOf(result, "dotnet_naming_style.parameters_style.capitalization"));
        if (separator is null)
            Assert.DoesNotContain(result.Properties, p => p.Name.EndsWith("word_separator", StringComparison.Ordinal));
        else
            Assert.Equal(separator, ValueOf(result, "dotnet_naming_style.parameters_style.word_separator"));
    }

    [Fact]
    public void Styles_UnrepresentableStyleIsSkipped()
    {
        // "AaBb_aaBb" mixes Pascal and camel segments, which one capitalization cannot express.
        var result = MapPolicies(("PredefinedNamingRules/=Parameters",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="AaBb_aaBb" />"""));

        Assert.Empty(result.Properties);
        Assert.Contains("AaBb_aaBb", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void ExtraRulesAreReportedAsAPartialConversion()
    {
        var result = MapPolicies(("PredefinedNamingRules/=Interfaces",
            """<Policy Inspect="True" Prefix="I" Suffix="" Style="AaBb"><ExtraRule Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.NotEmpty(result.Properties);
        Assert.Contains("alternative naming styles", Assert.Single(result.Warnings).Message);
    }

    // ---- User rules -------------------------------------------------------

    [Fact]
    public void UserRule_ReadsItsSymbolGroupFromTheDescriptor()
    {
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Static" AccessRightKinds="Public, Internal" Description="Public constants"><ElementKinds><Kind Name="CONSTANT_FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Empty(result.Skipped);
        Assert.Equal("field", ValueOf(result, "dotnet_naming_symbols.public_constants_symbols.applicable_kinds"));
        Assert.Equal("public, internal",
            ValueOf(result, "dotnet_naming_symbols.public_constants_symbols.applicable_accessibilities"));
        Assert.Equal("const, static",
            ValueOf(result, "dotnet_naming_symbols.public_constants_symbols.required_modifiers"));
    }

    [Fact]
    public void UserRule_MixedKindsDoNotRequireAModifierOnlySomeOfThemHave()
    {
        // FIELD implies nothing and READONLY_FIELD implies readonly; requiring "readonly"
        // would wrongly exclude plain fields.
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Private" Description="Fields"><ElementKinds><Kind Name="FIELD" /><Kind Name="READONLY_FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Equal("field", ValueOf(result, "dotnet_naming_symbols.fields_symbols.applicable_kinds"));
        Assert.DoesNotContain(result.Properties,
            p => p.Name.EndsWith("required_modifiers", StringComparison.Ordinal));
    }

    [Fact]
    public void UserRule_InstanceOnlyIsWidenedAndReported()
    {
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Instance" AccessRightKinds="Private" Description="Instance fields"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.NotEmpty(result.Properties);
        Assert.DoesNotContain(result.Properties, p => p.Name.EndsWith("required_modifiers", StringComparison.Ordinal));
        Assert.Contains("instance members only", Assert.Single(result.Warnings).Message);
    }

    [Fact]
    public void UserRule_UnrecognisedAccessibilityIsNeverWidenedToEverything()
    {
        // "No" is not an accessibility EditorConfig knows. Falling back to "*" would apply
        // the rule to every symbol in the codebase, which is far worse than skipping it.
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="No" AccessRightKinds="No" Description="Odd"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Empty(result.Properties);
        Assert.Contains("accessibility", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void UserRule_PartlyUnrecognisedAccessibilityKeepsTheRestAndReports()
    {
        // "FileLocal" is C# 11's file-local accessibility, which Roslyn's naming rules
        // cannot name. Dropping it narrows the rule, which is safe, but must be reported.
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Internal, Public, FileLocal" Description="Types"><ElementKinds><Kind Name="CLASS" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Empty(result.Skipped);
        Assert.Equal("internal, public", ValueOf(result, "dotnet_naming_symbols.types_symbols.applicable_accessibilities"));
        Assert.Contains("FileLocal", Assert.Single(result.Warnings).Message);
    }

    [Theory]
    [InlineData("ANY_FIELD", "field")]
    [InlineData("LOCAL", "local")]
    public void UserRule_GenericElementKinds(string kind, string expected)
    {
        var result = MapPolicies(("UserRules/=abc",
            $"""<Policy><Descriptor Staticness="Any" AccessRightKinds="Any" Description="Group"><ElementKinds><Kind Name="{kind}" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Empty(result.Skipped);
        Assert.Equal(expected, ValueOf(result, "dotnet_naming_symbols.group_symbols.applicable_kinds"));
    }

    [Fact]
    public void UserRule_AnyAccessibilityBecomesStar()
    {
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Any" Description="Types"><ElementKinds><Kind Name="CLASS" /><Kind Name="STRUCT" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Equal("class, struct", ValueOf(result, "dotnet_naming_symbols.types_symbols.applicable_kinds"));
        Assert.Equal("*", ValueOf(result, "dotnet_naming_symbols.types_symbols.applicable_accessibilities"));
    }

    [Fact]
    public void UserRule_UnknownElementKindIsSkippedAndNamed()
    {
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Any" Description="Odd"><ElementKinds><Kind Name="CSS_SELECTOR" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Empty(result.Properties);
        Assert.Contains("CSS_SELECTOR", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void UserRule_UnitySerialisedFieldOnly_IsSkippedByName()
    {
        // Unity projects are a target audience, so the reason has to name the kind.
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Any" Description="Unity"><ElementKinds><Kind Name="UNITY_SERIALISED_FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Empty(result.Properties);
        Assert.Contains("UNITY_SERIALISED_FIELD", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void UserRule_PartlyUnsupportedKinds_KeepsTheRestAndReports()
    {
        var result = MapPolicies(("UserRules/=abc",
            """<Policy><Descriptor Staticness="Any" AccessRightKinds="Public" Description="Unity fields"><ElementKinds><Kind Name="UNITY_SERIALISED_FIELD" /><Kind Name="PROPERTY" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Empty(result.Skipped);
        Assert.Equal("property", ValueOf(result, "dotnet_naming_symbols.unity_fields_symbols.applicable_kinds"));
        Assert.Contains("UNITY_SERIALISED_FIELD", Assert.Single(result.Warnings).Message);
    }

    [Fact]
    public void PredefinedRule_LocalsMapsToLocalVariables()
    {
        var result = MapPolicies(("PredefinedNamingRules/=Locals",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" />"""));

        Assert.Empty(result.Skipped);
        Assert.Equal("local", ValueOf(result, "dotnet_naming_symbols.locals_symbols.applicable_kinds"));
    }

    [Fact]
    public void PredefinedRule_OtherIsSkippedWithItsOwnReason()
    {
        // ReSharper's catch-all rule; .NET naming conventions have no "everything else".
        var result = MapPolicies(("PredefinedNamingRules/=Other",
            """<Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" />"""));

        Assert.Empty(result.Properties);
        Assert.Contains("catch-all", Assert.Single(result.Skipped).Reason);
    }

    [Fact]
    public void MalformedPolicyIsSkippedNotThrown()
    {
        var result = MapPolicies(("PredefinedNamingRules/=Parameters", "not xml"));

        Assert.Empty(result.Properties);
        Assert.Contains("not valid XML", Assert.Single(result.Skipped).Reason);
    }

    // ---- Duplicates -------------------------------------------------------

    [Fact]
    public void EquivalentRuleFromUserRulesIsReportedAsADuplicate()
    {
        // ReSharper writes the same rule under both keys; only one may be emitted.
        var result = MapPolicies(
            ("PredefinedNamingRules/=PrivateInstanceFields",
                """<Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" />"""),
            ("UserRules/=abc",
                """<Policy><Descriptor Staticness="Instance" AccessRightKinds="Private" Description="Instance fields (private)"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Contains("duplicate", Assert.Single(result.Skipped).Reason);
        Assert.DoesNotContain(result.Properties,
            p => p.Name.Contains("instance_fields_private", StringComparison.Ordinal));
    }

    [Fact]
    public void ADroppedDuplicateDoesNotAlsoReportAWarningAboutItself()
    {
        var result = MapPolicies(
            ("PredefinedNamingRules/=PrivateInstanceFields",
                """<Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" />"""),
            ("UserRules/=abc",
                """<Policy><Descriptor Staticness="Instance" AccessRightKinds="Private" Description="Instance fields (private)"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" /></Policy>"""));

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void ConflictingRuleOverTheSameSymbolsKeepsTheFirst()
    {
        var result = MapPolicies(
            ("PredefinedNamingRules/=Parameters",
                """<Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" />"""),
            ("UserRules/=abc",
                """<Policy><Descriptor Staticness="Any" AccessRightKinds="Any" Description="Params"><ElementKinds><Kind Name="PARAMETER" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="p" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Contains("conflicts with", Assert.Single(result.Skipped).Reason);
        Assert.Equal("camel_case", ValueOf(result, "dotnet_naming_style.parameters_style.capitalization"));
    }

    [Fact]
    public void DistinctRulesWithTheSameNameGetDistinctIdentifiers()
    {
        var result = MapPolicies(
            ("UserRules/=a",
                """<Policy><Descriptor Staticness="Any" AccessRightKinds="Private" Description="Fields"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" /></Policy>"""),
            ("UserRules/=b",
                """<Policy><Descriptor Staticness="Any" AccessRightKinds="Public" Description="Fields"><ElementKinds><Kind Name="FIELD" /></ElementKinds></Descriptor><Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" /></Policy>"""));

        Assert.Empty(result.Skipped);
        Assert.Contains(result.Properties, p => p.Name == "dotnet_naming_rule.fields.severity");
        Assert.Contains(result.Properties, p => p.Name == "dotnet_naming_rule.fields_2.severity");
    }

    // ---- Other languages --------------------------------------------------

    [Fact]
    public void VisualBasicNamingGoesIntoTheVisualBasicSection()
    {
        var result = SettingsMapper.Map([
            new DotSettingsEntry(
                "/Default/CodeStyle/Naming/VBNaming/PredefinedNamingRules/=Method/@EntryIndexedValue",
                DotSettingsValueType.String,
                """<Policy Inspect="True" Prefix="" Suffix="" Style="AaBb" />""")
        ]);

        Assert.NotEmpty(result.Properties);
        Assert.All(result.Properties, p => Assert.Equal("*.vb", p.Section));
    }

    [Fact]
    public void JavaScriptNamingIsSkipped()
    {
        var result = SettingsMapper.Map([
            new DotSettingsEntry(
                "/Default/CodeStyle/Naming/JavaScriptNaming/UserRules/=X/@EntryIndexedValue",
                DotSettingsValueType.String,
                """<Policy Inspect="True" Prefix="" Suffix="" Style="aaBb" />""")
        ]);

        Assert.Empty(result.Properties);
        Assert.Contains("JavaScriptNaming", Assert.Single(result.Skipped).Reason);
    }
}
