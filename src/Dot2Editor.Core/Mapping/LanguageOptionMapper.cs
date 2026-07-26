using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
/// The fallback that keeps a setting alive when no curated mapping exists: any option in a
/// known settings group becomes the "resharper_*" property ReSharper and Rider read
/// natively. Most ReSharper formatting options — blank lines and brace placement among
/// them — have no standard EditorConfig counterpart at all, so this is how they survive.
/// </summary>
internal static class LanguageOptionMapper
{
    internal static MapOutcome Map(SettingsKey key, LanguageProfile profile, DotSettingsEntry entry)
    {
        var option = key.After(profile.GroupPath);
        if (option.Contains("/=", StringComparison.Ordinal))
            return MapOutcome.Skipped("indexed (injected) entries are not supported");

        // Nested paths such as "ThisQualifier/INSTANCE_MEMBERS_QUALIFY_DECLARED_IN" are
        // only UI grouping; the option name is the last segment.
        var lastSlash = option.LastIndexOf('/');
        if (lastSlash >= 0) option = option[(lastSlash + 1)..];

        if (option.Length == 0) return MapOutcome.Skipped("key has no option name");

        return MapOutcome.Converted(new EditorConfigProperty(
            profile.Section,
            profile.PropertyPrefix + TextConventions.ToSnakeLower(option),
            ConvertValue(entry)));
    }

    private static string ConvertValue(DotSettingsEntry entry) => entry.ValueType switch
    {
        DotSettingsValueType.Boolean => entry.RawValue.Equals("True", StringComparison.OrdinalIgnoreCase)
            ? "true"
            : "false",
        DotSettingsValueType.Int64 or DotSettingsValueType.Double => entry.RawValue,
        _ => TextConventions.ToSnakeLower(entry.RawValue)
    };
}
