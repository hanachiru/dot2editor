<!-- Thanks for contributing. Keep this short — a couple of sentences is fine. -->

## What this changes

## Why

<!-- If this adds or changes a mapping, the two rules from CONTRIBUTING.md apply. -->

- [ ] The `.DotSettings` key was confirmed against a real file, and the EditorConfig
      property against JetBrains' or Microsoft's documentation — neither was guessed.
- [ ] Anything that cannot be converted is reported in `Skipped`, and anything converted
      only partially is reported in `Warnings`.
- [ ] `Comprehensive.DotSettings` exercises the new mapping, profile or skip rule.
- [ ] `dotnet test` passes, and the golden file was regenerated if the output changed.
