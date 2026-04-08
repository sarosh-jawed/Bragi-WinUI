# Bragi Test Cases

This document tracks the manual and automated validation approach for Bragi.

At Phase 1, the application has not been implemented yet, so this document establishes the baseline testing strategy and the future regression targets that later phases must satisfy.

---

## 1. Testing Goals

Bragi must be validated across:
- file loading
- input type detection
- CSV parsing
- subject extraction
- whitespace normalization
- blank handling
- categorization
- exclusion rules
- multi-match behavior
- uncategorized handling
- export generation
- run summary generation
- logging

---

## 2. Phase 1 Baseline Checks

These checks confirm the repository baseline is professional before coding begins.

### Repository Baseline
- `README.md` exists and describes the tool clearly
- `CHANGELOG.md` exists
- `LICENSE` exists
- `.editorconfig` exists
- `.gitignore` exists
- `docs/CONVENTIONS.md` exists
- `docs/TEST-CASES.md` exists
- `docs/SCREENSHOTS.md` exists

### Branching Baseline
- `main` remains stable
- phase work is done on a dedicated feature branch
- local working tree is clean before push

---

## 3. Planned Functional Test Areas

These are the minimum functional areas to validate once implementation begins.

### Plain Text Input
- one subject per line is loaded correctly
- blank lines are handled safely
- extra surrounding whitespace is normalized as configured

### CSV Input
- CSV file is recognized correctly
- configured subject column is selected correctly
- JSON-like subject arrays stored as text are extracted safely
- malformed or empty rows are counted and logged safely

### Categorization
- matching is case-insensitive when configured
- configured exclusions are respected
- fiction exclusions work correctly
- juvenile and children exclusions work correctly
- one subject may match multiple categories when enabled
- unmatched items route to uncategorized output

### Export
- every configured category file is generated
- uncategorized file is generated
- run summary file is generated
- preview counts match export counts
- summary counts match preview counts

### Logging
- pipeline stages are visible in logs
- warnings are meaningful
- support diagnostics are possible without exposing raw technical detail in the UI

---

## 4. Primary Regression Fixture

Primary planned regression fixture:
- `bragi_dummy_lcc_instance_items.csv`

Planned long-term location:
- `Bragi.Tests/Fixtures/bragi_dummy_lcc_instance_items.csv`

This fixture is intended to validate the complete Bragi pipeline:
- CSV loading
- subject extraction
- normalization
- categorization
- exclusions
- multi-match behavior
- uncategorized handling
- export generation
- run summary generation
- logging

### Expected benchmark totals for the dummy CSV
- total CSV rows = 30
- rows with non-empty subject arrays = 29
- rows with empty subjects = 1
- extracted subject entries = 80
- uncategorized subject entries = 6
- categorized assignments = 77
- multi-match subjects = 3

These values become a regression checkpoint once the implementation exists.

---

## 5. Must-Have Future Test Cases

The following tests must exist in later phases.

### Extraction Tests
- plain text extraction
- CSV extraction from configured column
- empty subject row handling
- whitespace normalization
- duplicate subject counting

### Categorization Tests
- art subject matches art
- fiction subject routes only to fiction when required
- juvenile subject is excluded from normal categories
- children exclusion is applied at subject level, not row level
- multi-category match is preserved when enabled
- uncategorized subject is routed correctly
- mixed-case text still matches
- extra spaces still match

### Export Tests
- per-category file generation
- uncategorized file generation
- run summary generation
- deterministic output ordering where configured
- preview and export count reconciliation

### Logging Tests
- app startup logging
- config load logging
- extraction completion logging
- categorization completion logging
- export completion logging

---

## 6. Notes

This document will become more detailed as implementation progresses.

In later phases, this file should include:
- exact manual execution steps
- expected results
- screenshot references
- links to regression fixtures
- links to automated test coverage
