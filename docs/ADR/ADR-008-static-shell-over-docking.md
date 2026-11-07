# ADR-008: Static Shell Over Docking

**Status:** Accepted (2026)
**Supersedes:** [ADR-004](ADR-004-docksuite-over-tabcontrol.md)

## Context
ADR-004 adopted DockPanelSuite for floating documents, document groups, persisted
layout and dockable tool windows. In practice the application used a small slice
of that feature set: a left tool strip, one document area and a results pane per
document. The docking library added a second tab host alongside the legacy
`TabControl`/`SplitContainer` path that still existed in `BaseWindow.Designer.cs`
and `TabManagerService`, so every feature had to be written twice and the two
hosts drifted apart.

The legacy static path is still complete and is what most lookups (editor panel,
splitter, results grid) already assume. Restoring it removes an external
dependency and leaves one source of truth for layout.

## Decision
Remove DockPanelSuite and host the shell with plain WinForms controls.

- `BaseWindow.Designer.cs` owns the layout: `splitContainer1` with `_leftTabs`
  (Database, Files, Variables, Outline, Git) on the left and `_tabControlMain`
  in Panel2.
- SQL editors are `TabPage` + `SplitContainer`; results (`Diagnostics`, `Log`,
  result grids) live in that document's `SplitContainer.Panel2`.
- Non-SQL documents (Preferences, Query Watch, History, Git diff) are ordinary
  `TabPage`s identified by `TabPageDocumentTag` and excluded from editor-only
  paths (save, close prompts, SQL shortcuts).
- AI Chat is a collapsible right-hand `SplitContainer` panel opened from the
  Settings → Panels menu.
- Tab order is user-managed: the main strip and each results strip support
  drag/drop reordering (`BaseWindow.MoveTabPage`).
- Session restore uses the existing `simpleStartup.manysql(.enc)` bundle.
  `dockLayout.xml` is no longer read or written.
- WinForms-specific; clean layers (`JustData.Application`,
  `JustData.ViewModels`) do not reference any layout host.

## Consequences
+ One document/results host — fewer code paths, simpler theming and testing.
+ No `WeifenLuo` dependency; the architecture test in
  `StaticShellTabStripTests` guards the removal.
+ Drag/drop reordering is available without a docking engine.
- Floating documents, auto-hide and multi-group docking are gone. If they are
  needed later, they return as a dedicated feature rather than a global shell
  dependency.
- Layout persistence is limited to tab order and per-document connection state.
