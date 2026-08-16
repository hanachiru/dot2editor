namespace Dot2Editor.Core.Models;

/// <summary>
///     A setting that was converted, but not in full: EditorConfig could express most
///     of it and something specific had to be left behind. Distinct from
///     <see cref="SkippedEntry" />, which produced no output at all.
/// </summary>
/// <param name="Key">The .DotSettings key the warning is about.</param>
/// <param name="Message">What could not be represented, and what was emitted instead.</param>
public sealed record ConversionWarning(string Key, string Message);
