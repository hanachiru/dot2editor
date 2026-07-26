using System.Xml;
using System.Xml.Linq;
using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
///     Converts ReSharper naming rules into .NET naming conventions
///     (<c>dotnet_naming_rule</c> / <c>dotnet_naming_symbols</c> / <c>dotnet_naming_style</c>).
///     A .DotSettings naming entry holds a small XML policy as its value, in one of two shapes:
///     <code>
/// PredefinedNamingRules/=PrivateInstanceFields  &lt;Policy Inspect="True" Prefix="_" Suffix="" Style="aaBb" /&gt;
/// UserRules/=&lt;guid&gt;                            &lt;Policy&gt;&lt;Descriptor Staticness=".." AccessRightKinds=".."
///                                                 Description=".."&gt;&lt;ElementKinds&gt;&lt;Kind Name="FIELD" /&gt;..
/// </code>
///     Predefined rules imply their symbol group from the key; user rules describe it explicitly.
/// </summary>
internal static class NamingRuleMapper
{
    internal const string PredefinedPrefix = "PredefinedNamingRules/=";
    internal const string UserRulePrefix = "UserRules/=";

    private const string AllAccessibilities = "*";

    private const string NonPrivate = "public, internal, protected, protected_internal, private_protected";

    /// <summary>
    ///     ReSharper's built-in rule names, which carry their symbol group in the name alone.
    ///     Confirmed against real .DotSettings files and the descriptors ReSharper writes for
    ///     the equivalent user rules.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, SymbolGroup> Predefined =
        new Dictionary<string, SymbolGroup>(StringComparer.Ordinal)
        {
            ["TypesAndNamespaces"] = new("namespace, class, struct, enum, delegate", AllAccessibilities, ""),
            ["Interfaces"] = new("interface", AllAccessibilities, ""),
            ["TypeParameters"] = new("type_parameter", AllAccessibilities, ""),
            ["MethodPropertyEvent"] = new("method, property, event", AllAccessibilities, ""),
            ["Method"] = new("method", AllAccessibilities, ""),
            ["Property"] = new("property", AllAccessibilities, ""),
            ["Event"] = new("event", AllAccessibilities, ""),
            ["Parameters"] = new("parameter", AllAccessibilities, ""),
            ["LocalVariables"] = new("local", AllAccessibilities, ""),
            ["Locals"] = new("local", AllAccessibilities, ""),
            ["LocalConstants"] = new("local", AllAccessibilities, "const"),
            ["LocalFunctions"] = new("local_function", AllAccessibilities, ""),
            ["EnumMember"] = new("field", AllAccessibilities, ""),
            ["PublicFields"] = new("field", "public", ""),
            ["PrivateInstanceFields"] = new("field", "private", ""),
            ["PrivateStaticFields"] = new("field", "private", "static"),
            ["PrivateConstants"] = new("field", "private", "const"),
            ["PrivateStaticReadonly"] = new("field", "private", "static, readonly"),
            ["Constants"] = new("field", NonPrivate, "const"),
            ["StaticReadonly"] = new("field", NonPrivate, "static, readonly")
        };

    /// <summary>
    ///     Built-in rules that exist but cannot be expressed, with the reason to report.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> UnmappablePredefined =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Other"] = "the \"Other\" rule is ReSharper's catch-all for everything the other " +
                        "rules do not match, which EditorConfig has no way to express"
        };

    /// <summary>ReSharper element kinds, and the modifier each one implies.</summary>
    private static readonly IReadOnlyDictionary<string, (string Kind, string Modifier)> ElementKinds =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["NAMESPACE"] = ("namespace", ""),
            ["CLASS"] = ("class", ""),
            ["STRUCT"] = ("struct", ""),
            ["INTERFACE"] = ("interface", ""),
            ["ENUM"] = ("enum", ""),
            ["DELEGATE"] = ("delegate", ""),
            ["FIELD"] = ("field", ""),
            ["ANY_FIELD"] = ("field", ""),
            ["LOCAL"] = ("local", ""),
            ["READONLY_FIELD"] = ("field", "readonly"),
            ["CONSTANT_FIELD"] = ("field", "const"),
            ["ENUM_MEMBER"] = ("field", ""),
            ["PROPERTY"] = ("property", ""),
            ["METHOD"] = ("method", ""),
            ["ASYNC_METHOD"] = ("method", "async"),
            ["EVENT"] = ("event", ""),
            ["PARAMETER"] = ("parameter", ""),
            ["TYPE_PARAMETER"] = ("type_parameter", ""),
            ["LOCAL_VARIABLE"] = ("local", ""),
            ["LOCAL_CONSTANT"] = ("local", "const"),
            ["LOCAL_FUNCTION"] = ("local_function", "")
        };

    private static readonly IReadOnlyDictionary<string, string> Accessibilities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Private"] = "private",
            ["Public"] = "public",
            ["Internal"] = "internal",
            ["Protected"] = "protected",
            ["ProtectedInternal"] = "protected_internal",
            ["PrivateProtected"] = "private_protected",
            ["Local"] = "local"
        };

    /// <summary>ReSharper style names, as capitalization plus an optional word separator.</summary>
    private static readonly IReadOnlyDictionary<string, (string Capitalization, string WordSeparator)> Styles =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["AaBb"] = ("pascal_case", ""),
            ["aaBb"] = ("camel_case", ""),
            ["AA_BB"] = ("all_upper", "_"),
            ["aa_bb"] = ("all_lower", "_"),
            ["Aa_bb"] = ("first_word_upper", "_"),
            ["AaBb_AaBb"] = ("pascal_case", "_"),
            ["aaBb_aaBb"] = ("camel_case", "_")
        };

    /// <summary>Converts one naming entry. Returns null when nothing could be produced.</summary>
    /// <param name="ruleKey">The part after "=" in the key: a rule name or a GUID.</param>
    /// <param name="policyXml">The entry value, an XML policy fragment.</param>
    /// <param name="section">The .editorconfig section the rule belongs in.</param>
    /// <param name="isPredefined">True for PredefinedNamingRules, false for UserRules.</param>
    /// <param name="context">Shared state that keeps names unique and drops duplicate rules.</param>
    /// <param name="key">The full .DotSettings key, used when reporting warnings.</param>
    internal static MapOutcome Map(
        string ruleKey,
        string policyXml,
        string section,
        bool isPredefined,
        NamingRuleContext context,
        string key)
    {
        // Warnings are held back until the rule is known to survive deduplication:
        // a rule that is dropped must not also report a caveat about itself.
        var warnings = new List<ConversionWarning>();
        XElement root;
        try
        {
            root = XElement.Parse(policyXml);
        }
        catch (XmlException)
        {
            return MapOutcome.Skipped("naming policy is not valid XML");
        }

        var descriptor = root.Element("Descriptor");
        var policy = descriptor is null ? root : root.Elements("Policy").FirstOrDefault() ?? root;

        SymbolGroup group;
        string name;
        if (isPredefined)
        {
            if (UnmappablePredefined.TryGetValue(ruleKey, out var why)) return MapOutcome.Skipped(why);

            if (!Predefined.TryGetValue(ruleKey, out var predefined))
                return MapOutcome.Skipped($"unrecognised predefined naming rule \"{ruleKey}\"");

            group = predefined;
            name = ruleKey;
        }
        else
        {
            if (descriptor is null)
                return MapOutcome.Skipped("naming rule has no descriptor describing which symbols it applies to");

            var built = BuildGroup(descriptor, key, warnings, out var groupReason);
            if (built is null)
                return MapOutcome.Skipped(groupReason ?? "naming rule uses element kinds that EditorConfig cannot express");

            group = built;
            name = descriptor.Attribute("Description")?.Value ?? ruleKey;
        }

        var style = policy.Attribute("Style")?.Value ?? string.Empty;
        if (!Styles.TryGetValue(style, out var capitalization))
            return MapOutcome.Skipped($"naming style \"{style}\" has no EditorConfig equivalent");

        var prefix = policy.Attribute("Prefix")?.Value ?? string.Empty;
        var suffix = policy.Attribute("Suffix")?.Value ?? string.Empty;
        var severity = string.Equals(policy.Attribute("Inspect")?.Value, "False", StringComparison.OrdinalIgnoreCase)
            ? "none"
            : "warning";

        // ReSharper stores the same rule under both PredefinedNamingRules and UserRules.
        // Two rules over the same symbols would be redundant at best and ambiguous at worst,
        // so the first one wins and the second is reported.
        var groupKey = GroupSignature(section, group);
        var styleKey = $"{capitalization.Capitalization}|{prefix}|{suffix}|{capitalization.WordSeparator}|{severity}";
        if (context.Groups.TryGetValue(groupKey, out var existing))
            return MapOutcome.Skipped(string.Equals(existing.Style, styleKey, StringComparison.Ordinal)
                ? $"duplicate of naming rule \"{existing.RuleId}\", which covers the same symbols"
                : $"conflicts with naming rule \"{existing.RuleId}\" over the same symbols; the earlier rule was kept");

        if (policy.Elements("ExtraRule").Any())
            warnings.Add(new ConversionWarning(
                key,
                "alternative naming styles were dropped: EditorConfig allows only one style per rule"));

        var id = UniqueId(name, context.UsedIds);
        context.Groups[groupKey] = (id, styleKey);
        context.Warnings.AddRange(warnings);

        var properties = new List<EditorConfigProperty>
        {
            new(section, $"dotnet_naming_rule.{id}.severity", severity),
            new(section, $"dotnet_naming_rule.{id}.symbols", $"{id}_symbols"),
            new(section, $"dotnet_naming_rule.{id}.style", $"{id}_style"),
            new(section, $"dotnet_naming_symbols.{id}_symbols.applicable_kinds", group.Kinds),
            new(section, $"dotnet_naming_symbols.{id}_symbols.applicable_accessibilities", group.Accessibilities),
            new(section, $"dotnet_naming_style.{id}_style.capitalization", capitalization.Capitalization)
        };

        if (group.Modifiers.Length > 0)
            properties.Add(new EditorConfigProperty(
                section, $"dotnet_naming_symbols.{id}_symbols.required_modifiers", group.Modifiers));

        if (prefix.Length > 0)
            properties.Add(new EditorConfigProperty(section, $"dotnet_naming_style.{id}_style.required_prefix",
                prefix));

        if (suffix.Length > 0)
            properties.Add(new EditorConfigProperty(section, $"dotnet_naming_style.{id}_style.required_suffix",
                suffix));

        if (capitalization.WordSeparator.Length > 0)
            properties.Add(new EditorConfigProperty(
                section, $"dotnet_naming_style.{id}_style.word_separator", capitalization.WordSeparator));

        return MapOutcome.Converted(properties);
    }

    /// <summary>Order-independent identity of a symbol group, used to spot duplicate rules.</summary>
    private static string GroupSignature(string section, SymbolGroup group)
    {
        static string Normalise(string commaSeparated)
        {
            return string.Join(",", commaSeparated
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .OrderBy(value => value, StringComparer.Ordinal));
        }

        return $"{section}|{Normalise(group.Kinds)}|{Normalise(group.Accessibilities)}|{Normalise(group.Modifiers)}";
    }

    private static SymbolGroup? BuildGroup(
        XElement descriptor, string key, List<ConversionWarning> warnings, out string? reason)
    {
        reason = null;
        var kinds = new List<string>();
        var impliedModifiers = new List<HashSet<string>>();
        var unsupported = new List<string>();
        foreach (var kind in descriptor.Descendants("Kind"))
        {
            var kindName = kind.Attribute("Name")?.Value ?? string.Empty;
            if (!ElementKinds.TryGetValue(kindName, out var mapped))
            {
                // Unity's UNITY_SERIALISED_FIELD is the common case: EditorConfig has no
                // notion of it. Note it and keep whatever else the rule covers.
                if (!unsupported.Contains(kindName)) unsupported.Add(kindName);

                continue;
            }

            if (!kinds.Contains(mapped.Kind)) kinds.Add(mapped.Kind);

            impliedModifiers.Add(mapped.Modifier.Length == 0 ? [] : [mapped.Modifier]);
        }

        if (kinds.Count == 0)
        {
            reason = unsupported.Count > 0
                ? $"rule only covers element kind(s) {string.Join(", ", unsupported)}, " +
                  "which EditorConfig has no equivalent for"
                : "naming rule lists no element kinds";
            return null;
        }

        if (unsupported.Count > 0)
            warnings.Add(new ConversionWarning(
                key,
                $"element kind(s) {string.Join(", ", unsupported)} have no EditorConfig equivalent " +
                "and were left out of the rule"));

        // Only a modifier shared by every kind can be required; "FIELD or READONLY_FIELD"
        // must not become "readonly", which would exclude plain fields.
        var modifiers = impliedModifiers.Count == 0
            ? []
            : impliedModifiers.Aggregate(new HashSet<string>(impliedModifiers[0]), (acc, next) =>
            {
                acc.IntersectWith(next);
                return acc;
            });

        var staticness = descriptor.Attribute("Staticness")?.Value;
        if (string.Equals(staticness, "Static", StringComparison.Ordinal))
            modifiers.Add("static");
        else if (string.Equals(staticness, "Instance", StringComparison.Ordinal))
            warnings.Add(new ConversionWarning(
                key,
                "rule applies to instance members only; EditorConfig cannot exclude static members, " +
                "so it was widened to all members of that kind"));

        var accessibilities = ParseAccessibilities(
            descriptor.Attribute("AccessRightKinds")?.Value, out var unknownAccess);
        if (accessibilities is null)
        {
            reason = $"accessibility \"{string.Join(", ", unknownAccess)}\" has no EditorConfig equivalent";
            return null;
        }

        if (unknownAccess.Count > 0)
            warnings.Add(new ConversionWarning(
                key,
                $"accessibility {string.Join(", ", unknownAccess)} has no EditorConfig equivalent " +
                "and was left out, so the rule covers fewer symbols than in Rider"));

        return new SymbolGroup(string.Join(", ", kinds), accessibilities,
            string.Join(", ", modifiers.OrderBy(m => m, StringComparer.Ordinal)));
    }

    /// <summary>
    /// Returns null when nothing in the list can be expressed. Widening an unrecognised
    /// accessibility to "*" would silently apply the rule to every symbol, so it never
    /// falls back to that.
    /// </summary>
    private static string? ParseAccessibilities(string? raw, out List<string> unknown)
    {
        unknown = [];
        if (string.IsNullOrWhiteSpace(raw) || raw.Contains("Any", StringComparison.Ordinal))
            return AllAccessibilities;

        var mapped = new List<string>();
        foreach (var value in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (Accessibilities.TryGetValue(value, out var found))
                mapped.Add(found);
            else
                unknown.Add(value);

        return mapped.Count == 0 ? null : string.Join(", ", mapped);
    }

    /// <summary>A readable, unique entity name for the rule, e.g. "private_instance_fields".</summary>
    private static string UniqueId(string name, HashSet<string> usedIds)
    {
        var id = TextConventions.ToIdentifier(name);
        if (id.Length == 0) id = "naming_rule";

        var unique = id;
        for (var i = 2; !usedIds.Add(unique); i++) unique = $"{id}_{i}";

        return unique;
    }

    /// <summary>The symbols a rule applies to, in EditorConfig spelling.</summary>
    private sealed record SymbolGroup(string Kinds, string Accessibilities, string Modifiers);
}
