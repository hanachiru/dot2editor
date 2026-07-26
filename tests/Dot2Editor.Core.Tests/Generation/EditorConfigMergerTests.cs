using Dot2Editor.Core.Generation;
using Dot2Editor.Core.Models;

namespace Dot2Editor.Core.Tests.Generation;

public class EditorConfigMergerTests
{
    private static EditorConfigProperty Cs(string name, string value)
    {
        return new EditorConfigProperty("*.cs", name, value);
    }

    [Fact]
    public void UpdatesAnExistingPropertyInPlace()
    {
        const string existing = "root = true\n\n[*.cs]\nindent_size = 2\nmax_line_length = 80\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal("root = true\n\n[*.cs]\nindent_size = 4\nmax_line_length = 80\n", result.EditorConfigText);
        Assert.Equal((0, 1, 0), (result.Added, result.Updated, result.Unchanged));
    }

    [Fact]
    public void AppendsMissingPropertyToTheEndOfItsSection()
    {
        const string existing = "[*.cs]\nindent_size = 4\n\n[*.md]\nindent_size = 2\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("max_line_length", "120")]);

        Assert.Equal("[*.cs]\nindent_size = 4\nmax_line_length = 120\n\n[*.md]\nindent_size = 2\n",
            result.EditorConfigText);
        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void AppendsAWholeSectionWhenMissing()
    {
        const string existing = "root = true\n\n[*]\ncharset = utf-8\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4"), Cs("tab_width", "4")]);

        Assert.Equal("root = true\n\n[*]\ncharset = utf-8\n\n[*.cs]\nindent_size = 4\ntab_width = 4\n",
            result.EditorConfigText);
        Assert.Equal(2, result.Added);
    }

    [Fact]
    public void PreservesCommentsBlankLinesAndUnrelatedProperties()
    {
        const string existing = """
                                # hand written, keep me
                                root = true

                                [*.cs]
                                # indentation, agreed in RFC 12
                                indent_size = 2
                                ; a semicolon comment
                                dotnet_diagnostic.CA1000.severity = none

                                [*.py]
                                indent_size = 4

                                """;

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Contains("# hand written, keep me", result.EditorConfigText);
        Assert.Contains("# indentation, agreed in RFC 12", result.EditorConfigText);
        Assert.Contains("; a semicolon comment", result.EditorConfigText);
        Assert.Contains("dotnet_diagnostic.CA1000.severity = none", result.EditorConfigText);
        // The [*.py] section must keep its own indent_size.
        Assert.Contains("[*.py]\nindent_size = 4", result.EditorConfigText.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void OnlyTouchesTheMatchingSection()
    {
        const string existing = "[*.cs]\nindent_size = 2\n\n[*.vb]\nindent_size = 2\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal("[*.cs]\nindent_size = 4\n\n[*.vb]\nindent_size = 2\n", result.EditorConfigText);
    }

    [Fact]
    public void CountsUnchangedPropertiesSeparately()
    {
        const string existing = "[*.cs]\nindent_size = 4\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal((0, 0, 1), (result.Added, result.Updated, result.Unchanged));
        Assert.Equal(existing, result.EditorConfigText);
    }

    [Fact]
    public void MatchesPropertyNamesCaseInsensitively()
    {
        const string existing = "[*.cs]\nINDENT_SIZE = 2\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal("[*.cs]\nindent_size = 4\n", result.EditorConfigText);
        Assert.Equal(1, result.Updated);
    }

    [Fact]
    public void PreservesIndentationOfAnExistingProperty()
    {
        const string existing = "[*.cs]\n    indent_size = 2\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal("[*.cs]\n    indent_size = 4\n", result.EditorConfigText);
    }

    [Fact]
    public void PreservesCrlfLineEndings()
    {
        const string existing = "[*.cs]\r\nindent_size = 2\r\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4"), Cs("tab_width", "4")]);

        Assert.Equal("[*.cs]\r\nindent_size = 4\r\ntab_width = 4\r\n", result.EditorConfigText);
    }

    [Fact]
    public void InsertsBeforeTrailingBlankLinesOfASection()
    {
        const string existing = "[*.cs]\nindent_size = 4\n\n\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("tab_width", "4")]);

        Assert.Equal("[*.cs]\nindent_size = 4\ntab_width = 4\n\n\n", result.EditorConfigText);
    }

    [Fact]
    public void MergingIntoAnEmptyFileJustAddsTheSection()
    {
        var result = EditorConfigMerger.Merge(string.Empty, [Cs("indent_size", "4")]);

        Assert.Equal("[*.cs]\nindent_size = 4\n", result.EditorConfigText);
        Assert.Equal(1, result.Added);
    }

    [Fact]
    public void IsIdempotent()
    {
        const string existing = "root = true\n\n[*]\ncharset = utf-8\n";
        EditorConfigProperty[] properties = [Cs("indent_size", "4"), new("*", "charset", "utf-8")];

        var once = EditorConfigMerger.Merge(existing, properties);
        var twice = EditorConfigMerger.Merge(once.EditorConfigText, properties);

        Assert.Equal(once.EditorConfigText, twice.EditorConfigText);
        Assert.Equal(0, twice.Added);
        Assert.Equal(0, twice.Updated);
    }

    [Fact]
    public void DoesNotConfuseACommentedOutPropertyWithARealOne()
    {
        const string existing = "[*.cs]\n# indent_size = 2\n";

        var result = EditorConfigMerger.Merge(existing, [Cs("indent_size", "4")]);

        Assert.Equal("[*.cs]\n# indent_size = 2\nindent_size = 4\n", result.EditorConfigText);
    }
}
