# ADR-004: DockSuite Over Embedded TabControl

**Status:** Accepted (2026)

## Context
WinForms `TabControl` lacks: drag-out float, document groups, persisted layout, per-document tool windows. Early prototype used a custom tab strip with manual docking — brittle, 400+ lines of layout code.

## Decision
Use **DockSuite** (open-source, MIT, stable API, maintenance-mode) for all document/tool window management in `JustData` (WinForms layer).

- `DockPanel` hosts: SQL editor documents, result grids, object explorer, output log, find results.
- Layout persisted to `%LOCALAPPDATA%\JustyBaseLegacy\layout.xml` on close, restored on start.
- Each document = `DockContent` wrapping a UserControl (`SqlEditorControl`, `ResultGridControl`).
- Tool windows (Object Explorer, Output) = `DockContent` with `DockState.DockLeft`/`DockBottom`.
- WinForms-specific; clean layers (`JustData.Application`, `JustData.ViewModels`) do not reference DockSuite.

## Consequences
+ Professional docking UX (drag, float, tab groups, auto-hide) with ~20 lines of setup code.
+ Layout persistence out of the box.
+ No custom layout code to maintain.
- External dependency (MIT, stable but not actively maintained — suitable for a mature API).
- Clean layers cannot use `DockContent` — view models expose `IDocumentViewModel` with `Title`, `IsDirty`, `CloseCommand`; WinForms adapter wraps in `DockContent`.