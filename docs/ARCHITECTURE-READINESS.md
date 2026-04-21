# Bragi Architecture Readiness

This document records the Phase 17 readiness audit for Bragi.

## 1. Naming convention alignment

Bragi remains aligned with Hel-style repository and project naming:

- repository: `Bragi-WinUI`
- solution: `Bragi`
- projects:
  - `Bragi.Domain`
  - `Bragi.Application`
  - `Bragi.Infrastructure`
  - `Bragi.App.WinUI`
  - `Bragi.Tests`

## 2. Output path strategy

The config model still uses a Hel-style local-machine output root strategy:

- `%LOCALAPPDATA%\Bragi\Output\YYYY-MM`

Current UI behavior intentionally requires the user to choose the output folder explicitly before export. This keeps release behavior safe and predictable while preserving the same general local-path philosophy.

## 3. Service boundary readiness

The current layering remains reusable:

- UI invokes workflow behavior through view models and orchestrators
- input ingest, extraction, categorization, config loading, path resolution, and export live outside the UI layer
- infrastructure services remain reusable from non-UI callers later if needed

## 4. UI leakage check

The intended boundary remains intact:

- no core categorization logic in XAML pages
- no direct file-processing logic in page code-behind
- no infrastructure dependency direction violations
- no reverse project references

## 5. Workflow orchestrator future readiness

The workflow orchestrator remains a strong future seam because it already centralizes:

- input loading
- extraction
- preview generation
- export generation
- cancellation-aware execution
- session state transitions

This makes it a natural candidate for reuse inside a larger shell later.

## 6. Merge-friendly future questions

### Could Bragi later become a module inside a combined shell?
Yes. The app already has a clear step-based workflow and a bounded service surface.

### Could the categorization engine be reused by another tool?
Yes. The config-driven categorization path already lives outside the WinUI layer.

### Could the config model be shared or mirrored?
Yes. The config structure is already explicit and versionable.

## 7. Conclusion

Bragi is:

- standalone today
- merge-friendly tomorrow

Future ecosystem integration should be feasible without rewriting the core categorization flow.