namespace Dot2Editor.Core.Mapping;

/// <summary>
/// Settings groups that have no EditorConfig representation, each with the reason a user
/// will see. .DotSettings holds far more than formatting — window state, plugin options,
/// profiler settings — and naming every one of them is what keeps "no known mapping" rare
/// enough to be a signal that something is genuinely missing.
///
/// Order matters: the first matching prefix wins, so put specific paths above the group
/// they belong to.
/// </summary>
internal static class SkippedGroups
{
    internal static readonly (string Prefix, string Reason)[] All =
    [
        // --- Code style groups that are not formatting ------------------------
        // Naming rules themselves convert; what reaches this list are the other
        // entries in the group, such as "apply style to existing code" toggles.
        ("CodeStyle/Naming/", "not a naming rule, and has no EditorConfig equivalent"),
        // JetBrains documents EditorConfig properties for C#, C++, VB.NET, XMLDOC,
        // HTML, CSS, JS/TS, Razor, ShaderLab, XML and Protobuf — but not for XAML.
        ("CodeStyle/CodeFormatting/XamlFormatter/", "XAML formatting has no EditorConfig properties"),
        // Rider's Unity plugin: no documented EditorConfig properties, so its options
        // are reported rather than guessed at.
        ("CodeStyle/CodeFormatting/UnityCSharpFormatting/",
            "Unity plugin formatting has no documented EditorConfig properties"),
        ("CodeStyle/CSharpMemberOrderPattern/", "member order patterns have no EditorConfig equivalent"),
        ("CodeStyle/CSharpFileLayoutPatterns/", "file layout patterns have no EditorConfig equivalent"),
        ("CodeStyle/EditorConfig/", "IDE preference about EditorConfig itself, not a formatting setting"),
        ("CodeStyle/IntroduceVariableUseVar/", "refactoring setting, not a formatting setting"),
        ("CodeStyle/LiveTemplatesUseVar/", "live template setting, not a formatting setting"),
        ("CodeStyle/CppSortIncludes/", "C++ include sorting has no documented EditorConfig properties"),
        ("CodeStyle/CppIncludeDirective/", "C++ include handling has no documented EditorConfig properties"),
        ("CodeStyle/CppIntroduceType/", "refactoring setting, not a formatting setting"),
        ("CodeStyle/CppPreferForwardDeclaration/", "C++ analysis setting, not a formatting setting"),
        ("CodeStyle/CppCVQualsPlacement/", "C++ cv-qualifier placement has no documented EditorConfig properties"),
        ("CodeStyle/AgentSmith/", "AgentSmith spell-checker plugin setting, not a formatting setting"),
        ("CodeStyle/XamlStyler/", "XamlStyler plugin setting, not a ReSharper formatting setting"),
        ("CodeStyle/CodeCleanup/", "code cleanup profiles have no EditorConfig equivalent"),
        ("CodeStyle/FileHeader/", "file header text has no EditorConfig equivalent"),
        ("CodeStyle/Generate/", "code generation setting, not a formatting setting"),
        ("CodeStyle/EncapsulateField/", "refactoring setting, not a formatting setting"),

        // --- Inspections -------------------------------------------------------
        // Severities are handled before this list is consulted; what is left under
        // this group are solution-wide analysis switches.
        ("CodeInspection/Highlighting/", "solution-wide analysis setting, not a formatting setting"),
        ("CodeInspection/ExcludedFiles/", "excluded files have no EditorConfig equivalent"),
        ("CodeInspection/GeneratedCode/", "generated code settings have no EditorConfig equivalent"),
        ("CodeInspection/CodeAnnotations/", "code annotation settings have no EditorConfig equivalent"),
        // Catch-all for the rest: analysis engine and inspection-level switches.
        ("CodeInspection/", "code inspection setting, not a formatting setting"),

        // --- Editor behaviour and stored IDE state -----------------------------
        ("Environment/", "IDE environment setting, not a formatting setting"),
        ("Housekeeping/", "IDE state, not a formatting setting"),
        ("TimelineLayout/", "IDE state, not a formatting setting"),
        ("SnapshotsStore/", "IDE state, not a formatting setting"),
        ("SnapshotDilogManager/", "IDE state, not a formatting setting"),
        ("WpfWindowSize/", "IDE state, not a formatting setting"),
        ("OptionsGeneral/", "IDE preference, not a formatting setting"),
        ("HighlightingManager/", "IDE highlighting preference, not a formatting setting"),
        ("ReportingFeature/", "IDE reporting setting, not a formatting setting"),
        ("CodeEditing/", "editing behaviour setting, not a formatting setting"),
        ("PatternsAndTemplates/", "live templates and TODO patterns have no EditorConfig equivalent"),
        ("CustomTools/", "custom tool configuration, not a formatting setting"),

        // --- Spelling ----------------------------------------------------------
        ("UserDictionary/", "spell-checker dictionary, not a formatting setting"),
        ("InstalledDictionaries/", "spell-checker dictionary, not a formatting setting"),
        ("ReSpeller/", "spell-checker setting, not a formatting setting"),
        ("GrammarAndSpelling/", "grammar and spelling setting, not a formatting setting"),

        // --- Diagnostics, coverage and plugins ---------------------------------
        ("RiderDebugger/", "debugger setting, not a formatting setting"),
        ("SymbolServers/", "symbol server setting, not a formatting setting"),
        ("GlobalFilterSettingsManager/", "coverage/filter configuration, not a formatting setting"),
        ("FilterSettingsManager/", "coverage/filter configuration, not a formatting setting"),
        ("CoverageSessionsPersistenceManagerSettings/", "coverage session state, not a formatting setting"),
        ("dotCover/", "dotCover setting, not a formatting setting"),
        ("Profilers/", "profiler setting, not a formatting setting"),
        ("Profiling/", "profiler setting, not a formatting setting"),
        ("Dpa/", "dynamic program analysis setting, not a formatting setting"),
        ("Connection/", "connection list, not a formatting setting"),
        ("TeamCityAddin/", "TeamCity plugin setting, not a formatting setting"),
        ("StyleCopOptions/", "StyleCop plugin setting, not a ReSharper formatting setting"),
        ("GeneralLinter/", "linter plugin setting, not a formatting setting"),
        ("CppUnrealEngine/", "Unreal Engine plugin setting, not a formatting setting"),
        ("CppCodeCompletion/", "code completion setting, not a formatting setting")
    ];

    /// <summary>The reason this group is skipped, or null when it is not in the list.</summary>
    internal static string? ReasonFor(SettingsKey key)
    {
        foreach (var (prefix, reason) in All)
            if (key.StartsWith(prefix))
                return reason;

        return null;
    }
}
