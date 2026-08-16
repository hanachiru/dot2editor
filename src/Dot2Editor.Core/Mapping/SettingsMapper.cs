using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
/// Routes each .DotSettings entry to the mapping step that owns it. The steps are tried in
/// order, and the first one to claim an entry decides its fate:
/// <list type="number">
///     <item><see cref="InspectionSeverityMapper" /> — inspection and compiler-warning severities.</item>
///     <item><see cref="NamingRuleMapper" /> — naming rules, as .NET naming conventions.</item>
///     <item><see cref="SkippedGroups" /> — groups that have no EditorConfig equivalent.</item>
///     <item><see cref="KnownMappings" /> — curated standard EditorConfig / Roslyn properties.</item>
///     <item><see cref="LanguageOptionMapper" /> — the "resharper_*" fallback.</item>
/// </list>
/// An entry no step claims is reported as skipped, never dropped silently.
/// </summary>
public static class SettingsMapper
{
    private const string NamingGroupPrefix = "CodeStyle/Naming/";

    /// <summary>Naming groups whose rules .NET naming conventions can express.</summary>
    private static readonly IReadOnlyDictionary<string, string> NamingSections =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CSharpNaming"] = Sections.CSharp,
            ["VBNaming"] = Sections.VisualBasic
        };

    /// <summary>
    /// Translates .DotSettings entries into EditorConfig properties. One entry may produce
    /// several properties, and every entry that produces none is returned as skipped with a
    /// reason, so nothing disappears without explanation.
    /// </summary>
    /// <param name="entries">Entries from <see cref="Parsing.DotSettingsParser" />.</param>
    /// <returns>The properties to write, plus what was skipped or only partially converted.</returns>
    public static MappingResult Map(IEnumerable<DotSettingsEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var properties = new List<EditorConfigProperty>();
        var skipped = new List<SkippedEntry>();
        var warnings = new List<ConversionWarning>();
        var naming = new NamingRuleContext(warnings);

        foreach (var entry in entries)
        {
            var outcome = MapEntry(entry, naming);
            if (outcome.Properties is { } mapped)
                properties.AddRange(mapped);
            else
                skipped.Add(new SkippedEntry(
                    entry.Key, outcome.SkipReason ?? "no known mapping to an EditorConfig property"));
        }

        return new MappingResult(properties, skipped, warnings);
    }

    private static MapOutcome MapEntry(DotSettingsEntry entry, NamingRuleContext naming)
    {
        if (SettingsKey.IsTombstone(entry.Key))
            return MapOutcome.Skipped("entry is a tombstone marking a removed setting");

        if (!SettingsKey.TryParse(entry.Key, out var key))
            return MapOutcome.Skipped("key is not in the expected \"/Default/.../@Entry*Value\" form");

        // Severities and naming rules are checked before the skipped groups, because both
        // live inside groups whose remaining entries are IDE-only settings.
        if (key.StartsWith(InspectionSeverityMapper.GroupPrefix))
            return InspectionSeverityMapper.Map(key.After(InspectionSeverityMapper.GroupPrefix), entry.RawValue);

        if (key.StartsWith(NamingGroupPrefix) && MapNamingRule(key, entry, naming) is { IsHandled: true } named)
            return named;

        if (SkippedGroups.ReasonFor(key) is { } reason) return MapOutcome.Skipped(reason);

        var profile = LanguageProfile.Find(key.Path);

        if (KnownMappings.ByPath.TryGetValue(key.Path, out var expand))
        {
            if (expand(entry.RawValue) is { } curated) return MapOutcome.Converted(curated);

            // No standard equivalent for this value (e.g. "UseVarWhenEvident", which is
            // neither true nor false). ReSharper can still express it, so keep the setting
            // as a resharper_* property instead of dropping it.
            return profile is not null
                ? LanguageOptionMapper.Map(key, profile, entry)
                : MapOutcome.Skipped($"value \"{entry.RawValue}\" has no standard EditorConfig equivalent");
        }

        return profile is not null
            ? LanguageOptionMapper.Map(key, profile, entry)
            : MapOutcome.Skipped("no known mapping to an EditorConfig property");
    }

    /// <summary>
    /// Naming rules live at "CodeStyle/Naming/&lt;language&gt;Naming/(PredefinedNamingRules|UserRules)/=&lt;id&gt;".
    /// Anything else under the group is left for the skipped-group list to explain.
    /// </summary>
    private static MapOutcome MapNamingRule(SettingsKey key, DotSettingsEntry entry, NamingRuleContext naming)
    {
        var rest = key.After(NamingGroupPrefix);
        var slash = rest.IndexOf('/');
        if (slash < 0) return MapOutcome.NotHandled;

        var language = rest[..slash];
        var remainder = rest[(slash + 1)..];

        var isPredefined = remainder.StartsWith(NamingRuleMapper.PredefinedPrefix, StringComparison.Ordinal);
        var isUserRule = remainder.StartsWith(NamingRuleMapper.UserRulePrefix, StringComparison.Ordinal);
        if (!isPredefined && !isUserRule) return MapOutcome.NotHandled;

        if (!NamingSections.TryGetValue(language, out var section))
            return MapOutcome.Skipped($"{language} naming rules have no EditorConfig equivalent");

        var ruleId = remainder[(isPredefined
            ? NamingRuleMapper.PredefinedPrefix.Length
            : NamingRuleMapper.UserRulePrefix.Length)..];

        return NamingRuleMapper.Map(ruleId, entry.RawValue, section, isPredefined, naming, entry.Key);
    }
}
