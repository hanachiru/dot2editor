using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Generation;

/// <summary>
///     Merges generated properties into an existing .editorconfig without disturbing
///     it: comments, blank lines, property order, unrelated sections and unrelated
///     properties are all preserved byte for byte. Only the properties dot2editor
///     generates are updated in place, and missing ones are appended to their section.
///     Like the rest of Core this is pure text-in / text-out.
/// </summary>
public static class EditorConfigMerger
{
    /// <summary>
    ///     Folds properties into the text of an existing .editorconfig. A property that is
    ///     already present is updated where it stands; one that is missing is appended to its
    ///     section; a section that does not exist yet is appended to the file. Everything else
    ///     is left untouched, and the file's line endings are preserved. Merging the same
    ///     properties twice produces no further change.
    /// </summary>
    /// <param name="existingText">The current content of the .editorconfig file.</param>
    /// <param name="properties">The properties to fold in.</param>
    /// <returns>The merged text, plus how many properties were added, updated or already correct.</returns>
    public static MergeResult Merge(string existingText, IEnumerable<EditorConfigProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(existingText);
        ArgumentNullException.ThrowIfNull(properties);

        var newline = existingText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = SplitLines(existingText, out var endsWithNewline);
        int added = 0, updated = 0, unchanged = 0;

        foreach (var group in properties.GroupBy(p => p.Section, StringComparer.Ordinal))
        {
            var (order, wanted) = Resolve(group);

            var (start, end) = FindSection(lines, group.Key);
            if (start < 0)
            {
                AppendSection(lines, group.Key, order, wanted);
                added += order.Count;
                continue;
            }

            var pending = new List<string>();
            foreach (var name in order)
            {
                var index = FindProperty(lines, start + 1, end, name);
                if (index < 0)
                {
                    pending.Add($"{name} = {wanted[name]}");
                    continue;
                }

                var replacement = LeadingWhitespace(lines[index]) + $"{name} = {wanted[name]}";
                if (string.Equals(lines[index], replacement, StringComparison.Ordinal))
                {
                    unchanged++;
                }
                else
                {
                    lines[index] = replacement;
                    updated++;
                }
            }

            if (pending.Count > 0)
            {
                // Append after the section's last real line, not after its trailing blanks.
                var insertAt = end;
                while (insertAt - 1 > start && string.IsNullOrWhiteSpace(lines[insertAt - 1])) insertAt--;

                lines.InsertRange(insertAt, pending);
                added += pending.Count;
            }
        }

        var text = string.Join(newline, lines) + (endsWithNewline ? newline : string.Empty);
        return new MergeResult(text, added, updated, unchanged);
    }

    /// <summary>
    /// The properties a section should end up with, in the order they were first seen.
    /// Within a section the last occurrence of a name wins, matching the generator.
    /// </summary>
    private static (List<string> Order, Dictionary<string, string> Wanted) Resolve(
        IEnumerable<EditorConfigProperty> section)
    {
        var wanted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        foreach (var property in section)
        {
            if (!wanted.ContainsKey(property.Name)) order.Add(property.Name);

            wanted[property.Name] = property.Value;
        }

        return (order, wanted);
    }

    private static List<string> SplitLines(string text, out bool endsWithNewline)
    {
        if (text.Length == 0)
        {
            endsWithNewline = true;
            return [];
        }

        var lines = text.ReplaceLineEndings("\n").Split('\n').ToList();
        endsWithNewline = lines.Count > 1 && lines[^1].Length == 0;
        if (endsWithNewline) lines.RemoveAt(lines.Count - 1);

        return lines;
    }

    /// <summary>Returns the header index and the exclusive end index of a section, or (-1, -1).</summary>
    private static (int Start, int End) FindSection(List<string> lines, string section)
    {
        var header = $"[{section}]";
        for (var i = 0; i < lines.Count; i++)
        {
            if (!string.Equals(lines[i].Trim(), header, StringComparison.Ordinal)) continue;

            var end = i + 1;
            while (end < lines.Count && !IsSectionHeader(lines[end])) end++;

            return (i, end);
        }

        return (-1, -1);
    }

    private static int FindProperty(List<string> lines, int from, int to, string name)
    {
        for (var i = from; i < to; i++)
            if (PropertyName(lines[i]) is { } found && string.Equals(found, name, StringComparison.OrdinalIgnoreCase))
                return i;

        return -1;
    }

    private static void AppendSection(
        List<string> lines, string section, List<string> order, Dictionary<string, string> values)
    {
        if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1])) lines.Add(string.Empty);

        lines.Add($"[{section}]");
        lines.AddRange(order.Select(name => $"{name} = {values[name]}"));
    }

    private static bool IsSectionHeader(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith('[') && trimmed.EndsWith(']');
    }

    /// <summary>The property name on a line, or null if the line is blank, a comment or a header.</summary>
    private static string? PropertyName(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';') ||
            IsSectionHeader(trimmed)) return null;

        var equals = trimmed.IndexOf('=');
        if (equals <= 0) return null;

        var name = trimmed[..equals].TrimEnd();
        return name.Length == 0 ? null : name;
    }

    private static string LeadingWhitespace(string line)
    {
        var i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;

        return line[..i];
    }
}
