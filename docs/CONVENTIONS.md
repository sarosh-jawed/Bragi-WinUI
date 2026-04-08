# Bragi Conventions

This document defines the baseline working conventions for the Bragi repository.

The goal is to keep the codebase professional, predictable, and aligned with Hel-level standards before implementation begins.

---

## 1. Repository and Solution Naming

Repository name:
- `Bragi-WinUI`

Solution name:
- `Bragi`

Planned project names:
- `Bragi.Domain`
- `Bragi.Application`
- `Bragi.Infrastructure`
- `Bragi.App.WinUI`
- `Bragi.Tests`

Namespace rule:
- namespaces should mirror the project and folder structure
- avoid namespace drift
- keep names explicit and business-meaningful

---

## 2. Dependency Direction

Dependency direction is non-negotiable.

Allowed project reference flow:
- `Bragi.Domain` -> no project references
- `Bragi.Application` -> references `Bragi.Domain`
- `Bragi.Infrastructure` -> references `Bragi.Application` and `Bragi.Domain`
- `Bragi.App.WinUI` -> references `Bragi.Application`, `Bragi.Infrastructure`, and `Bragi.Domain`
- `Bragi.Tests` -> references whichever projects are required for testing

Rules:
- no reverse references
- no UI project referenced by non-UI projects
- no infrastructure dependency from domain
- no shortcuts that break layering just to get something working quickly

---

## 3. Naming Rules

General naming:
- use clear, explicit names
- avoid vague utility-style names
- prefer business language over generic helper naming

Types:
- classes, records, enums, and interfaces use PascalCase
- interfaces begin with `I`
- private fields use `_camelCase`
- locals and parameters use `camelCase`

Files:
- one primary type per file
- file name should match the primary type name

Examples:
- `SubjectEntry.cs`
- `CategorizationResult.cs`
- `IConfigProvider.cs`
- `WorkflowOrchestrator.cs`

---

## 4. UI and Code-Behind Rules

The UI must stay thin.

Rules:
- no core business logic in code-behind
- no direct file I/O from workflow pages
- no hard-coded category rules in UI
- no category processor logic inside XAML page classes
- UI should delegate behavior through services, orchestrators, and view models

The WinUI layer is responsible for:
- presentation
- navigation
- binding
- user feedback
- invoking application services

It is not responsible for:
- extraction logic
- categorization logic
- export formatting
- config rule evaluation

---

## 5. Configuration Rules

Bragi is configuration-driven.

Rules:
- category rules belong in configuration, not processor classes
- input schema settings belong in configuration
- output options belong in configuration
- local environment overrides belong in `config.local.json`
- defaults should remain safe for public repository use

Planned configuration sources:
- packaged `config.json`
- optional `%LOCALAPPDATA%\Bragi\config.local.json`

---

## 6. Logging Style

Logging should be structured, useful, and supportable.

Preferred logging stack:
- `ILogger<T>`
- Serilog file sink

Planned log root:
- `%LOCALAPPDATA%\Bragi\Logs`

Logging rules:
- log major workflow steps
- log counts and outcomes
- log warnings with useful context
- keep user-facing messages clean
- keep technical exception detail in logs
- do not log noise just to increase volume

At minimum, later implementation should log:
- app launch
- config load
- input selection
- extraction start and completion
- categorization start and completion
- export start and completion
- warnings
- handled exceptions

---

## 7. Output Folder Strategy

Default output root:
- `%LOCALAPPDATA%\Bragi\Output\YYYY-MM`

Rules:
- outputs should be deterministic
- output file names should remain legacy-compatible unless intentionally changed
- user override of output folder is allowed in the UI
- local machine-specific paths should not be hard-coded in committed source

Expected compatibility targets:
- per-category text outputs
- `NotCategorizedSubjects.txt`
- `RunSummary.txt`

---

## 8. Testing Rules

Testing is not optional.

Rules:
- no skipping categorization tests just because matching logic is configuration-driven
- edge cases must be tested deliberately
- regression behavior must be documented
- preview counts, export counts, and summary counts should reconcile

The dummy CSV fixture will be used as a primary regression benchmark in later phases.

---

## 9. Git and Branching Rules

Branching model:
- `main` is for stable code only
- create one feature branch per phase
- merge only after review, successful build, and successful test where applicable

Commit style:
- keep commit messages clear and scoped
- prefer professional summaries such as:
  - `docs: establish phase 1 repo baseline`
  - `build: scaffold Bragi solution`
  - `feat: add configuration loader`
  - `test: add categorization regression coverage`

---

## 10. Non-Negotiables

These rules should remain in effect throughout the project:
- no reverse project references
- no hard-coded category rules in processor classes
- no direct file I/O from UI pages
- no leaking UI logic into infrastructure
- no rushed shortcuts that make future phases harder to maintain

These conventions exist so Bragi stays clean today and remains merge-friendly tomorrow.
