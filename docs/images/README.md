# Documentation screenshots

PNG files in this folder are used by the root [README.md](../../README.md).

Current set (light + dark): `login`, `preferences`, `explorer`, `editor-showcase`.

You can replace any PNG by hand — filenames must stay the same for README links to keep working.

## Regenerate (FlaUI)

Prerequisites: Windows desktop, `test_nz_connection` in `%AppData%\JustyBaseLegacy\credentials.json.enc`, Netezza reachable.

Preferences shots use the real menu path (docked tab in the main editor), not a standalone form.

```powershell
dotnet test tests/JustData.UiTests/JustData.UiTests.csproj -c Release `
  --filter "Category=DocumentationScreenshots"
```
