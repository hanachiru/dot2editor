namespace Dot2Editor.Core.Models;

/// <summary>
///     A single entry read from a .DotSettings file.
/// </summary>
/// <param name="Key">The raw x:Key value, e.g. "/Default/CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE/@EntryValue".</param>
/// <param name="ValueType">The CLR type declared by the XML element (s:String, s:Boolean, ...).</param>
/// <param name="RawValue">The element text as-is, e.g. "True" or "4".</param>
public sealed record DotSettingsEntry(string Key, DotSettingsValueType ValueType, string RawValue);

/// <summary>
///     The CLR type a .DotSettings entry declares through its XML element name,
///     for example <c>&lt;s:Boolean&gt;</c> or <c>&lt;s:Int64&gt;</c>.
/// </summary>
public enum DotSettingsValueType
{
    /// <summary>A textual value, usually a ReSharper enum such as "NEXT_LINE".</summary>
    String,

    /// <summary>A boolean value, spelled "True" or "False" in the file.</summary>
    Boolean,

    /// <summary>A whole number, such as an indent size.</summary>
    Int64,

    /// <summary>A fractional number, such as a continuous indent multiplier.</summary>
    Double,

    /// <summary>Any other element type; the value is treated as text.</summary>
    Other
}
