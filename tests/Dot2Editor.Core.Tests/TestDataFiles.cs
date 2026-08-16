namespace Dot2Editor.Core.Tests;

/// <summary>Access to the files under TestData/, which are copied next to the test assembly.</summary>
internal static class TestDataFiles
{
    internal static string Sample => Read("Sample.DotSettings");

    internal static string Comprehensive => Read("Comprehensive.DotSettings");

    internal static string EdgeCases => Read("EdgeCases.DotSettings");

    internal static string PathTo(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
    }

    internal static string Read(string fileName)
    {
        return File.ReadAllText(PathTo(fileName));
    }
}
