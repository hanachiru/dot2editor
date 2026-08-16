using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
///     Curated mappings from a .DotSettings key path to standard EditorConfig /
///     Roslyn (.NET coding convention) properties, which are understood by every
///     editor rather than only by ReSharper and Rider.
///     The dictionary key is the path between "/Default/" and the trailing
///     "/@EntryValue", e.g. "CodeStyle/CSharpVarKeywordUsage/ForBuiltInTypes".
///     Anything not listed here falls back to a "resharper_*" property; see
///     <see cref="SettingsMapper" />.
/// </summary>
internal static class KnownMappings
{
    private const string Cs = Sections.CSharp;

    internal static readonly IReadOnlyDictionary<string, Expand> ByPath =
        new Dictionary<string, Expand>(StringComparer.Ordinal)
        {
            // --- Core EditorConfig properties ---------------------------------
            ["CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE"] = One(Cs, "indent_size", Integer),
            ["CodeStyle/CodeFormatting/CSharpFormat/TAB_WIDTH"] = One(Cs, "tab_width", Integer),
            ["CodeStyle/CodeFormatting/CSharpFormat/INDENT_STYLE"] = One(Cs, "indent_style", IndentStyle),
            ["CodeStyle/CodeFormatting/CSharpFormat/WRAP_LIMIT"] = One(Cs, "max_line_length", Integer),

            // --- "var" vs explicit type (csharp_style_var_*) -------------------
            ["CodeStyle/CSharpVarKeywordUsage/ForBuiltInTypes"] =
                One(Cs, "csharp_style_var_for_built_in_types", VarPreference),
            ["CodeStyle/CSharpVarKeywordUsage/ForSimpleTypes"] =
                One(Cs, "csharp_style_var_when_type_is_apparent", VarPreference),
            ["CodeStyle/CSharpVarKeywordUsage/ForOtherTypes"] =
                One(Cs, "csharp_style_var_elsewhere", VarPreference),

            // --- int vs Int32 (dotnet_style_predefined_type_*) -----------------
            ["CodeStyle/CodeFormatting/CSharpCodeStyle/BUILT_IN_TYPE_REFERENCE_STYLE"] =
                One(Cs, "dotnet_style_predefined_type_for_locals_parameters_members", PredefinedType),
            ["CodeStyle/CodeFormatting/CSharpCodeStyle/BUILT_IN_TYPE_REFERENCE_STYLE_FOR_MEMBER_ACCESS"] =
                One(Cs, "dotnet_style_predefined_type_for_member_access", PredefinedType),

            // --- "this." qualification (dotnet_style_qualification_for_*) ------
            // A single flags value such as "Field, Property, Event, Method"
            // expands into one property per member kind.
            ["CodeStyle/CodeFormatting/CSharpCodeStyle/ThisQualifier/INSTANCE_MEMBERS_QUALIFY_MEMBERS"] =
                QualificationFlags,

            // --- Spaces --------------------------------------------------------
            // JetBrains gives this option a different EditorConfig name from its
            // .DotSettings key, so the generic resharper_* fallback would invent a
            // property that does not exist.
            ["CodeStyle/CodeFormatting/CSharpFormat/SPACE_AFTER_TYPECAST_PARENTHESES"] =
                One(Cs, "csharp_space_after_cast", Boolean),

            // --- Braces (csharp_prefer_braces) ---------------------------------
            // ReSharper stores one setting per statement kind; EditorConfig has a
            // single property, so they must agree to be emitted. See SettingsMapper.
            ["CodeStyle/CodeFormatting/CSharpCodeStyle/BRACES_FOR_IFELSE"] =
                One(Cs, "csharp_prefer_braces", Braces)
        };

    /// <summary>Member kinds that INSTANCE_MEMBERS_QUALIFY_MEMBERS can list.</summary>
    private static readonly (string Flag, string Property)[] QualificationKinds =
    [
        ("Field", "dotnet_style_qualification_for_field"),
        ("Property", "dotnet_style_qualification_for_property"),
        ("Method", "dotnet_style_qualification_for_method"),
        ("Event", "dotnet_style_qualification_for_event")
    ];

    private static Expand One(string section, string name, Func<string, string?> convert)
    {
        return raw => convert(raw) is { } value
            ? [new EditorConfigProperty(section, name, value)]
            : null;
    }

    private static IReadOnlyList<EditorConfigProperty>? QualificationFlags(string raw)
    {
        var flags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // "None" or an empty value means "do not qualify anything".
        var known = flags.Where(f => !f.Equals("None", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (known.Any(f => !QualificationKinds.Any(k => k.Flag.Equals(f, StringComparison.OrdinalIgnoreCase))))
            return null;

        return QualificationKinds
            .Select(kind => new EditorConfigProperty(
                Cs,
                kind.Property,
                known.Contains(kind.Flag, StringComparer.OrdinalIgnoreCase) ? "true" : "false"))
            .ToArray();
    }

    private static string? Integer(string raw)
    {
        return long.TryParse(raw, out _) ? raw : null;
    }

    private static string? Boolean(string raw)
    {
        return raw switch
        {
            "True" => "true",
            "False" => "false",
            _ => null
        };
    }

    private static string? IndentStyle(string raw)
    {
        return raw switch
        {
            "Tab" or "Tabs" => "tab",
            "Space" or "Spaces" => "space",
            _ => null
        };
    }

    // ReSharper: UseVar / UseVarWhenEvident / UseExplicitType.
    // "UseVarWhenEvident" is a third state that true/false cannot express.
    private static string? VarPreference(string raw)
    {
        return raw switch
        {
            "UseVar" => "true",
            "UseExplicitType" => "false",
            _ => null
        };
    }

    private static string? PredefinedType(string raw)
    {
        return raw switch
        {
            "UseKeyword" or "USE_KEYWORD" => "true",
            "UseType" or "USE_TYPE" => "false",
            _ => null
        };
    }

    // ReSharper: Required / NotRequired / NotRequiredForBoth / RequiredForMultiline.
    private static string? Braces(string raw)
    {
        return raw switch
        {
            "Required" => "true",
            "NotRequired" or "NotRequiredForBoth" => "false",
            "RequiredForMultiline" => "when_multiline",
            _ => null
        };
    }

    /// <summary>
    ///     Converts one raw .DotSettings value into the properties it implies.
    ///     Returns null when the value has no EditorConfig equivalent.
    /// </summary>
    internal delegate IReadOnlyList<EditorConfigProperty>? Expand(string rawValue);
}
