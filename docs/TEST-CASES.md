# Bragi Test Cases

This document tracks the current validation strategy for Bragi and separates what is already implemented from what is intentionally deferred.

---

## 1. Current Validation Scope

Bragi currently needs validation across:

- file loading
- input type detection
- CSV parsing
- subject extraction
- whitespace normalization
- blank handling
- duplicate counting
- categorization
- exclusion rules
- multi-match behavior
- uncategorized handling
- export generation
- run summary generation
- workflow/session state
- navigation state consistency
- logging visibility

---

## 2. Current Automated Test Coverage

The repository already includes automated tests in these areas:

### Extraction
- `Bragi.Tests/Extraction/SubjectExtractionServiceTests.cs`

Current automated coverage includes:
- plain text extraction
- blank line handling
- duplicate counting
- CSV extraction from configured columns
- JSON-array CSV cell extraction
- source row and metadata preservation

### Categorization
- `Bragi.Tests/Categorization/CategorizationServiceTests.cs`

Current automated coverage includes:
- case, punctuation, and whitespace normalization
- multi-match behavior
- deterministic single-match behavior when multi-match is disabled
- fiction exclusion
- juvenile exclusion
- uncategorized routing
- blank-after-normalization handling

### Export
- `Bragi.Tests/Export/TextExportServiceTests.cs`

Current automated coverage includes:
- category file generation
- uncategorized file generation
- run summary file generation
- deterministic export behavior

### Workflow
- `Bragi.Tests/Workflow/StepNavigationServiceTests.cs`
- `Bragi.Tests/Workflow/WizardSessionStoreTests.cs`
- `Bragi.Tests/Workflow/WorkflowOrchestratorTests.cs`
- `Bragi.Tests/Workflow/WizardStateTests.cs`

Current automated coverage includes:
- linear step navigation
- step-lock behavior
- session persistence across navigation
- preview/export clearing when upstream state changes
- busy-operation cancellation handling
- initial wizard-state consistency

---

## 3. Current Manual Smoke Checks

These checks should be run after meaningful UI or workflow changes.

### Shell startup
- app launches successfully from `Bragi.App.WinUI`
- shell opens to the Start page
- 5-step navigation is visible
- later steps remain locked until the workflow advances

### Load Input
- `.txt` file can be selected
- `.csv` file can be selected
- detected input kind updates correctly
- extraction metrics refresh after loading

### Review Subjects
- extracted subject preview appears
- duplicate count appears
- blank or ignored count appears
- parse warning count appears
- review completion unlocks Preview Results

### Preview Results
- preview generation succeeds after review completion
- category counts appear
- category grouping appears
- uncategorized list appears when applicable
- routing reasons appear in preview items

### Export & Finish
- output folder can be selected
- export completes successfully
- generated file list appears
- output folder can be opened
- log folder can be opened
- run summary text appears in the page

---

## 4. Intentionally Deferred Work

The following work is intentionally not part of this pre-Phase-14 cleanup branch:

### Full legacy category-rule completion
The packaged `config.json` currently contains only a starter set of category rules.

That means the app is not yet ready for full legacy-compatible count validation against the complete Bragi dummy CSV regression benchmark.

### Full dummy CSV regression fixture integration
Primary target fixture:

- `bragi_dummy_lcc_instance_items.csv`

Planned long-term repository location:

- `Bragi.Tests/Fixtures/bragi_dummy_lcc_instance_items.csv`

This fixture should be finalized only when the category rules are expanded to the intended legacy-compatible set.

---

## 5. Planned Full Regression Targets After Rule Completion

Once the category rules are completed, Bragi should be validated against the main dummy CSV benchmark for:

- total CSV rows = 30
- rows with non-empty subject arrays = 29
- rows with empty subjects = 1
- extracted subject entries = 80
- uncategorized subject entries = 6
- categorized assignments = 77
- multi-match subjects = 3

That future regression pass should validate:

- extraction counts
- preview counts
- export line counts
- run summary counts
- logging visibility
- category-by-category expected totals

---

## 6. Notes

This file now reflects the current implemented codebase rather than the earlier planning-only state.

It should be expanded later with:

- exact manual execution steps for the dummy CSV regression fixture
- screenshot references
- expected file outputs by category
- category-by-category benchmark counts
- release-readiness validation cases
