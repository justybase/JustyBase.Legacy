# ADR-001: Clean Layers Separation (net10.0 without -windows)

**Status:** Accepted (2026)

## Context
WinForms projects typically couples business logic to UI, making unit testing difficult. We want testable application logic and view models without spinning up a Windows desktop.

## Decision
- `JustData.Application` targets `net10.0` (no `-windows`), contains **only** interfaces, DTOs, use cases, and pure C# services.
- `JustData.ViewModels` targets `net10.0` (no `-windows`), references `CommunityToolkit.Mvvm` only, contains view models, commands, and `IMessenger`-based messaging.
- `JustData` (WinForms) targets `net10.0-windows`, references both clean layers and all provider projects (`App.Data.*`, `AppBase.*`).
- Architecture tests (`tests/JustData.ViewModels.Tests/ArchitectureTests.cs`) use reflection to forbid any reference to `System.Windows.Forms` / `System.Drawing` / `Microsoft.Win32` in `JustData.Application` and `JustData.ViewModels`.

## Consequences
+ Unit tests run headless on Linux/macOS runners (no desktop required).
+ Clear dependency direction: **UI → ViewModels → Application** (enforced by tests).
+ View models stay platform-agnostic; WinForms-specific adapters (file pickers, clipboard, DPI) live in `JustData` via small interfaces.
- Extra adapter layer for every WinForms-specific operation (file dialog, clipboard, print, etc.).
- Developers must remember not to add `using System.Windows.Forms;` in clean layers — architecture test catches it in CI.