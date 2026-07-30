# SQL typing performance — reproduction & analysis

Env-gated span logger for locating BIG.SQL keystroke lag in Legacy (FCTB).

## Automated (preferred)

Assumes **BIG.SQL is restored on startup** (your normal JustData config). FlaUI only logs in, waits for BIG in the title, types ~20 s, then ranks spans.

```powershell
dotnet test tests\JustData.UiTests\JustData.UiTests.csproj -c Debug --filter "FullyQualifiedName~SqlTypingPerfFlaUiTests"
```

The test sets `JUSTYBASE_SQL_TYPING_PERF=1` on the child process.

Artifacts:

- `%LocalAppData%\JustyBase\perf\sql-typing-spans-*.ndjson`
- `%LocalAppData%\JustyBase\perf\typing-rank-*.txt`

Optional fallback (only if startup does not restore BIG): `--ui-test-open-file=<path>`.

## Enable (manual)

```powershell
$env:JUSTYBASE_SQL_TYPING_PERF = "1"
```

Output: `%LocalAppData%\JustyBase\perf\sql-typing-spans-yyyyMMdd-HHmmss.ndjson`

## Protocol A — typing (manual)

1. Close other JustData instances; Rebuild; start with env set (BIG.SQL opens itself).
2. Wait for idle UI.
3. Type 60 s (letters + Enter). No save/run.
4. Exit app (flushes `session_summary`).
5. Run analyzer below.

## Protocol B — selection (optional)

10 s caret/selection only; compare `host.selection_delayed`.

## Span map

| op | Hypothesis |
|----|------------|
| fctb.core / wordwrap / syntax_highlight | A |
| fctb.subscribers | B/H |
| editor.handle_text_changed / host.fctb_text_changed | B |
| host.fctb_text_changed_delayed* / autocomplete.* | C |
| host.selection_delayed | D |
| host.semantic | E |

If all spans under ~20 ms but UI freezes → F (paint/GDI); see `vs-confirm.md`.

## Analyze

```powershell
powershell -File tools\Analyze-SqlTypingSpans.ps1
```
