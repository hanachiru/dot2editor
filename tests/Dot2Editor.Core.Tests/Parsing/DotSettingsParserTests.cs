using Dot2Editor.Core.Models;
using Dot2Editor.Core.Parsing;

namespace Dot2Editor.Core.Tests.Parsing;

public class DotSettingsParserTests
{
    private const string Wrapper = """
                                   <wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">{0}</wpf:ResourceDictionary>
                                   """;

    [Fact]
    public void Parse_ReadsKeyTypeAndValue()
    {
        var xml = string.Format(Wrapper,
            """<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE/@EntryValue">4</s:Int64>""");

        var entries = DotSettingsParser.Parse(xml);

        var entry = Assert.Single(entries);
        Assert.Equal("/Default/CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE/@EntryValue", entry.Key);
        Assert.Equal(DotSettingsValueType.Int64, entry.ValueType);
        Assert.Equal("4", entry.RawValue);
    }

    [Theory]
    [InlineData("String", DotSettingsValueType.String)]
    [InlineData("Boolean", DotSettingsValueType.Boolean)]
    [InlineData("Int64", DotSettingsValueType.Int64)]
    [InlineData("Double", DotSettingsValueType.Double)]
    [InlineData("Decimal", DotSettingsValueType.Other)]
    public void Parse_MapsElementNameToValueType(string elementName, DotSettingsValueType expected)
    {
        var xml = string.Format(Wrapper,
            $"""<s:{elementName} x:Key="/Default/Whatever/@EntryValue">x</s:{elementName}>""");

        var entries = DotSettingsParser.Parse(xml);

        Assert.Equal(expected, Assert.Single(entries).ValueType);
    }

    [Fact]
    public void Parse_IgnoresElementsWithoutKey()
    {
        var xml = string.Format(Wrapper, "<s:String>orphan</s:String>");

        Assert.Empty(DotSettingsParser.Parse(xml));
    }

    [Fact]
    public void Parse_InvalidXml_Throws()
    {
        Assert.Throws<DotSettingsParseException>(() => DotSettingsParser.Parse("not xml at all"));
    }

    [Fact]
    public void Parse_RejectsDocumentTypeDefinitions()
    {
        // SECURITY.md states that external entity attacks do not apply because DTDs are
        // rejected. This pins that behaviour rather than trusting the framework default.
        const string xxe = """
                           <?xml version="1.0"?>
                           <!DOCTYPE root [<!ENTITY secret SYSTEM "file:///etc/passwd">]>
                           <wpf:ResourceDictionary xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                             <s:String x:Key="/Default/Whatever/@EntryValue">&secret;</s:String>
                           </wpf:ResourceDictionary>
                           """;

        Assert.Throws<DotSettingsParseException>(() => DotSettingsParser.Parse(xxe));
    }

    [Theory]
    [InlineData("﻿")]
    [InlineData("﻿﻿")]
    [InlineData("\n  ")]
    public void Parse_ToleratesLeadingByteOrderMarksAndWhitespace(string prefix)
    {
        // Real .DotSettings files have been found with two BOMs; reading the file strips
        // one, and the leftover used to make the document unreadable.
        var xml = prefix + string.Format(Wrapper,
            """<s:Int64 x:Key="/Default/CodeStyle/CodeFormatting/CSharpFormat/INDENT_SIZE/@EntryValue">4</s:Int64>""");

        var entry = Assert.Single(DotSettingsParser.Parse(xml));

        Assert.Equal("4", entry.RawValue);
    }

    [Fact]
    public void Parse_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DotSettingsParser.Parse(null!));
    }
}
