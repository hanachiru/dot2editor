using System.Globalization;
using System.Text;

namespace Dot2Editor.Core.Mapping;

/// <summary>
/// Translations between the way JetBrains spells things and the way EditorConfig does.
/// </summary>
internal static class TextConventions
{
    /// <summary>
    /// Converts a ReSharper spelling to an EditorConfig one:
    /// "NEXT_LINE" → "next_line", "ChopIfLong" → "chop_if_long".
    /// </summary>
    internal static string ToSnakeLower(string value)
    {
        var builder = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0 && value[i - 1] != '_' && !char.IsUpper(value[i - 1]))
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>
    /// JetBrains escapes non-alphanumeric characters in keys as "_00XX" hex, so
    /// "UnusedMember_002EGlobal" is "UnusedMember.Global".
    /// </summary>
    internal static string DecodeEscapes(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
            if (value[i] == '_' && i + 4 < value.Length && value[i + 1] == '0' && value[i + 2] == '0' &&
                int.TryParse(value.AsSpan(i + 3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var code))
            {
                builder.Append((char)code);
                i += 4;
            }
            else
            {
                builder.Append(value[i]);
            }

        return builder.ToString();
    }

    /// <summary>
    /// Whether a name can be written as an EditorConfig property. Guards against emitting
    /// keys that no editor can read, such as an id that still contains "::".
    /// </summary>
    internal static bool IsWritablePropertyName(string name) =>
        name.Length > 0 && name.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    /// <summary>
    /// Turns free text into a lower_snake_case identifier, dropping anything that is not a
    /// letter or digit: "Static readonly fields (private)" → "static_readonly_fields_private".
    /// </summary>
    internal static string ToIdentifier(string text)
    {
        var builder = new StringBuilder(text.Length);
        var lastWasSeparator = true;
        foreach (var c in text)
            if (char.IsLetterOrDigit(c))
            {
                if (char.IsUpper(c) && !lastWasSeparator) builder.Append('_');

                builder.Append(char.ToLowerInvariant(c));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                builder.Append('_');
                lastWasSeparator = true;
            }

        return builder.ToString().Trim('_');
    }
}
