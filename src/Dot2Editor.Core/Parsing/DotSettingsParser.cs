using System.Xml;
using System.Xml.Linq;
using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Parsing;

/// <summary>
///     Parses the XML of a .DotSettings file (a WPF ResourceDictionary) into flat entries.
///     Pure string-in / objects-out: no file system access.
/// </summary>
public static class DotSettingsParser
{
    private static readonly XNamespace XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    ///     A .DotSettings file is written by Rider and never needs a DTD, so document type
    ///     definitions are refused outright and no resolver is supplied. That rules out
    ///     external entity attacks and entity-expansion denial of service explicitly, rather
    ///     than relying on whatever the runtime happens to default to.
    /// </summary>
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        CloseInput = true
    };

    /// <summary>
    /// Removes leading byte order marks and whitespace. Reading a file usually strips one
    /// BOM, but real .DotSettings files have been seen with two, which leaves a stray
    /// U+FEFF in the string and makes the XML reader reject the document.
    /// </summary>
    private static string StripByteOrderMarks(string text)
    {
        var start = 0;
        while (start < text.Length && (text[start] == '﻿' || char.IsWhiteSpace(text[start])))
        {
            start++;
        }

        return start == 0 ? text : text[start..];
    }

    /// <summary>
    ///     Reads every keyed entry from a .DotSettings document, in file order.
    ///     Elements without an <c>x:Key</c> attribute are ignored.
    /// </summary>
    /// <param name="dotSettingsXml">The contents of a .DotSettings file.</param>
    /// <returns>The entries found, which may be empty.</returns>
    /// <exception cref="DotSettingsParseException">The input is not a readable .DotSettings document.</exception>
    public static IReadOnlyList<DotSettingsEntry> Parse(string dotSettingsXml)
    {
        ArgumentNullException.ThrowIfNull(dotSettingsXml);

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(new StringReader(StripByteOrderMarks(dotSettingsXml)), ReaderSettings);
            document = XDocument.Load(reader);
        }
        catch (XmlException e)
        {
            throw new DotSettingsParseException($"Input is not valid XML: {e.Message}", e);
        }

        var root = document.Root
                   ?? throw new DotSettingsParseException("Input has no root element.");

        var entries = new List<DotSettingsEntry>();
        foreach (var element in root.Elements())
        {
            var key = element.Attribute(XamlNs + "Key")?.Value;
            if (string.IsNullOrEmpty(key)) continue;

            entries.Add(new DotSettingsEntry(key, ParseValueType(element.Name.LocalName), element.Value));
        }

        return entries;
    }

    private static DotSettingsValueType ParseValueType(string elementLocalName)
    {
        return elementLocalName switch
        {
            "String" => DotSettingsValueType.String,
            "Boolean" => DotSettingsValueType.Boolean,
            "Int64" => DotSettingsValueType.Int64,
            "Double" => DotSettingsValueType.Double,
            _ => DotSettingsValueType.Other
        };
    }
}
