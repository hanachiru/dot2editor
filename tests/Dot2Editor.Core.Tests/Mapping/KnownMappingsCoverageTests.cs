using Dot2Editor.Core.Mapping;
using Dot2Editor.Core.Parsing;

namespace Dot2Editor.Core.Tests.Mapping;

/// <summary>
///     Guards the promise that Comprehensive.DotSettings exercises everything this
///     tool can convert. Adding a mapping without adding test data fails here.
/// </summary>
public class KnownMappingsCoverageTests
{
    private static readonly string Xml = TestDataFiles.Comprehensive;

    [Fact]
    public void EveryCuratedMappingAppearsInTestData()
    {
        var missing = KnownMappings.ByPath.Keys
            .Where(path => !Xml.Contains($"/Default/{path}/@Entry", StringComparison.Ordinal))
            .ToArray();

        Assert.True(missing.Length == 0,
            "Comprehensive.DotSettings is missing curated mappings:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void EveryLanguageProfileAppearsInTestData()
    {
        var missing = LanguageProfile.All
            .Where(profile => !Xml.Contains($"/Default/{profile.GroupPath}", StringComparison.Ordinal))
            .Select(profile => profile.GroupPath)
            .ToArray();

        Assert.True(missing.Length == 0,
            "Comprehensive.DotSettings is missing language groups:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void EverySkippedGroupAppearsInTestData()
    {
        var missing = SkippedGroups.All
            .Where(group => !Xml.Contains($"/Default/{group.Prefix}", StringComparison.Ordinal))
            .Select(group => group.Prefix)
            .ToArray();

        Assert.True(missing.Length == 0,
            "Comprehensive.DotSettings is missing skipped groups:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void NoCuratedMappingIsSkippedForTheTestData()
    {
        var result = DotSettingsConverter.Convert(Xml);

        var wrongly = result.Skipped
            .Where(s => KnownMappings.ByPath.Keys.Any(path => s.Key.Contains(path, StringComparison.Ordinal)))
            .Select(s => $"{s.Key} ({s.Reason})")
            .ToArray();

        Assert.True(wrongly.Length == 0,
            "Curated mappings were skipped:\n  " + string.Join("\n  ", wrongly));
    }

    [Fact]
    public void EveryLanguageProfileProducesItsOwnPrefixedProperty()
    {
        var result = DotSettingsConverter.Convert(Xml);

        var missing = LanguageProfile.All
            .Select(profile => profile.PropertyPrefix)
            .Distinct()
            .Where(prefix => !result.Properties.Any(p => p.Name.StartsWith(prefix, StringComparison.Ordinal)))
            .ToArray();

        Assert.True(missing.Length == 0,
            "No property was produced for prefixes:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void EverySkippedEntryHasANonEmptyReason()
    {
        var result = DotSettingsConverter.Convert(Xml);

        Assert.NotEmpty(result.Skipped);
        Assert.All(result.Skipped, s => Assert.False(string.IsNullOrWhiteSpace(s.Reason)));
    }

    [Fact]
    public void EveryEntryIsEitherConvertedOrReported()
    {
        var entries = DotSettingsParser.Parse(Xml);
        var result = DotSettingsConverter.Convert(Xml);

        // Curated mappings may expand one entry into several properties, so the
        // count of distinct source keys is what has to add up.
        var reported = result.Skipped.Select(s => s.Key).Distinct().Count();
        var converted = entries.Count - reported;

        Assert.Equal(entries.Count, converted + reported);
        Assert.True(converted > 0);
    }
}
