using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
///     State shared by every naming rule in one conversion. EditorConfig naming entities are
///     referenced by name, so identifiers must stay unique, and ReSharper writes the same rule
///     twice — once under PredefinedNamingRules and once under UserRules — so equivalent rules
///     have to be recognised and emitted only once.
/// </summary>
internal sealed class NamingRuleContext(List<ConversionWarning> warnings)
{
    /// <summary>Entity names already emitted.</summary>
    internal HashSet<string> UsedIds { get; } = new(StringComparer.Ordinal);

    /// <summary>Symbol group signature → the rule that claimed it, and its style signature.</summary>
    internal Dictionary<string, (string RuleId, string Style)> Groups { get; } = new(StringComparer.Ordinal);

    internal List<ConversionWarning> Warnings { get; } = warnings;
}
