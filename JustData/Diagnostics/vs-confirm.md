# VS CPU Usage confirmation (after NDJSON ranking)

Use only **after** in-app spans name a dominant `op` / module.

## Steps

1. Debug → Performance Profiler → **CPU Usage**.
2. Start JustData with `JUSTYBASE_SQL_TYPING_PERF=1` (optional; NDJSON already has the segment).
3. Open BIG.SQL; record **20–30 s** of the same typing scenario.
4. Stop collection.
5. In **Functions**, filter to the module matching the top span:
   - Dominant `fctb.*` / `editor.handle_*` → `JustyBase.Legacy.TextBox.dll`
   - Dominant `host.*` → `JustyBaseLegacy.dll` / `JustData`
   - Dominant `autocomplete.*` → `AppBase.Services.dll`
6. Sort by **Self CPU** (not Inclusive alone).
7. In **Call Tree**, expand the top 1–2 functions with highest Self Time.
8. Export Functions view to CSV and keep next to the NDJSON file.

## Hypothesis F (spans low, UI still freezes)

If NDJSON shows no slow spans but freezes remain:

- Prefer **Events** / **UI Analysis** (VS 18.x if available).
- Look for `OnPaint`, `Invalidate`, `System.Drawing`, `gdi32!`, `user32!`.
- That points to paint/invalidation volume, not lint/NetezzaSql.
