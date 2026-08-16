using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Mapping;

/// <summary>
/// What one mapping step made of an entry. Three states, so a step can say "this is mine
/// and here is the result", "this is mine and here is why it cannot be converted", or
/// "not mine, keep looking" without overloading nulls.
/// </summary>
internal readonly struct MapOutcome
{
    private MapOutcome(IReadOnlyList<EditorConfigProperty>? properties, string? skipReason)
    {
        Properties = properties;
        SkipReason = skipReason;
    }

    /// <summary>The step did not recognise the entry; routing should continue.</summary>
    internal static MapOutcome NotHandled => default;

    internal IReadOnlyList<EditorConfigProperty>? Properties { get; }

    internal string? SkipReason { get; }

    /// <summary>True once a step has claimed the entry, whether or not it converted.</summary>
    internal bool IsHandled => Properties is not null || SkipReason is not null;

    internal static MapOutcome Converted(IReadOnlyList<EditorConfigProperty> properties) => new(properties, null);

    internal static MapOutcome Converted(EditorConfigProperty property) => new([property], null);

    internal static MapOutcome Skipped(string reason) => new(null, reason);
}
