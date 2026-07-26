using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
/// Converts ReSharper inspection severities.
///
/// Most become "resharper_&lt;id&gt;_highlighting". Compiler warnings are recorded under the
/// same group as e.g. "CSharpWarnings::CS0109", and those become Roslyn's
/// "dotnet_diagnostic.CS0109.severity", which the compiler itself honours on build.
/// </summary>
internal static class InspectionSeverityMapper
{
    internal const string GroupPrefix = "CodeInspection/Highlighting/InspectionSeverities/=";

    /// <summary>Severities in the spelling ReSharper's own properties use.</summary>
    private static readonly IReadOnlyDictionary<string, string> ResharperSeverities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ERROR"] = "error",
            ["WARNING"] = "warning",
            ["SUGGESTION"] = "suggestion",
            ["HINT"] = "hint",
            ["DO_NOT_SHOW"] = "do_not_show"
        };

    /// <summary>The same severities in Roslyn's spelling, for dotnet_diagnostic.</summary>
    private static readonly IReadOnlyDictionary<string, string> DiagnosticSeverities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ERROR"] = "error",
            ["WARNING"] = "warning",
            ["SUGGESTION"] = "suggestion",
            ["HINT"] = "silent",
            ["DO_NOT_SHOW"] = "none"
        };

    private static readonly (string Prefix, string Section)[] CompilerWarnings =
    [
        ("CSharpWarnings::", Sections.CSharp),
        ("VBWarnings::", Sections.VisualBasic)
    ];

    internal static MapOutcome Map(string encodedId, string rawValue)
    {
        var severity = rawValue.Trim();
        if (severity.Length == 0) return MapOutcome.Skipped("entry has no value");

        var id = TextConventions.DecodeEscapes(encodedId);

        foreach (var (prefix, section) in CompilerWarnings)
            if (id.StartsWith(prefix, StringComparison.Ordinal))
                return MapCompilerWarning(id[prefix.Length..], section, severity, rawValue);

        if (!ResharperSeverities.TryGetValue(severity, out var mapped))
            return MapOutcome.Skipped($"unknown inspection severity \"{rawValue}\"");

        var name = TextConventions.ToSnakeLower(id.Replace('.', '_'));

        // Never emit a key EditorConfig cannot represent; report it instead.
        return TextConventions.IsWritablePropertyName(name)
            ? MapOutcome.Converted(new EditorConfigProperty(Sections.Any, $"resharper_{name}_highlighting", mapped))
            : MapOutcome.Skipped($"inspection id \"{id}\" has no valid EditorConfig property name");
    }

    private static MapOutcome MapCompilerWarning(
        string diagnosticId, string section, string severity, string rawValue)
    {
        if (!DiagnosticSeverities.TryGetValue(severity, out var mapped))
            return MapOutcome.Skipped($"unknown inspection severity \"{rawValue}\"");

        return diagnosticId.Length > 0 && diagnosticId.All(char.IsLetterOrDigit)
            ? MapOutcome.Converted(
                new EditorConfigProperty(section, $"dotnet_diagnostic.{diagnosticId}.severity", mapped))
            : MapOutcome.Skipped($"\"{diagnosticId}\" is not a diagnostic id EditorConfig can name");
    }
}
