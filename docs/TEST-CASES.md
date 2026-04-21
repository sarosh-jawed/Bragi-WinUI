# Bragi Test Cases

This document tracks the automated and manual validation approach for Bragi as of Phase 15.

---

## 1. Scope

Bragi must now be trusted across:

- config loading and validation
- input type detection
- CSV parsing
- plain text extraction
- CSV subject extraction
- whitespace normalization
- duplicate counting
- categorization behavior
- fiction exclusion behavior
- juvenile exclusion behavior
- multi-category matching
- uncategorized routing
- export file generation
- run summary generation
- workflow state integrity
- user-selected output folder behavior

---

## 2. Automated Coverage

Automated tests live in `Bragi.Tests`.

### Config validation
- actual app `config.json` loads successfully
- category rules validate successfully
- legacy rule set keys are present
- output file names remain unique

### Input ingestion
- `.txt` files detect as plain text
- `.csv` files detect as CSV
- unsupported extensions return unknown when fallback is disabled
- unsupported extensions return plain text when fallback is enabled
- CSV rows load correctly, including duplicate header handling

### Subject extraction
- plain text extraction works
- blank lines are ignored safely
- duplicates are counted
- CSV JSON-array extraction works
- semicolon-delimited subject strings split correctly when JSON-array mode is disabled

### Categorization
- art subject matches art
- fiction subject only routes to fiction when normal category disables fiction
- juvenile subject is excluded from normal categories
- one subject may match multiple categories
- uncategorized subject routes correctly
- blank-after-normalization subject routes to uncategorized correctly
- mixed case text still matches
- extra spaces still match

### Export
- category files are generated
- uncategorized file is generated
- run summary file is generated
- dummy fixture export produces the expected file set

### Workflow
- wizard state remains consistent
- step locking works correctly
- cancellation leaves session state stable
- export completion state is preserved correctly

### Regression fixture
Primary committed regression fixture:

- `Bragi.Tests/Fixtures/bragi_dummy_lcc_instance_items.csv`

Expected benchmark totals:

- total CSV rows = 30
- rows with non-empty subject arrays = 29
- rows with empty subjects = 1
- extracted subject entries = 80
- uncategorized subject entries = 6
- categorized assignments = 77
- multi-match subjects = 3

---

## 3. Manual Validation Cases

Use the app for these manual checks before release.

### Case 1: Plain text input
1. Launch Bragi.
2. Go to **Load Input**.
3. Choose a `.txt` file with one subject per line.
4. Confirm the detected kind is **PlainText**.
5. Confirm extracted subject counts appear correctly.
6. Proceed through preview and export.

Expected result:
- extraction succeeds
- preview succeeds
- export succeeds
- run summary matches the plain text input

### Case 2: CSV extraction
1. Launch Bragi.
2. Choose the dummy CSV fixture.
3. Confirm the detected kind is **Csv**.
4. Confirm extraction shows:
   - total records read = 30
   - extracted subjects = 80
   - ignored blanks = 1
   - duplicates = 10
   - parse warnings = 0

Expected result:
- extraction metrics match the benchmark

### Case 3: Duplicate subject case
1. Load a file with repeated subject values.
2. Confirm duplicate count increases.
3. Confirm extraction still succeeds.

Expected result:
- duplicates are counted without blocking processing

### Case 4: Fiction exclusion case
1. Load a subject set containing a non-fiction keyword plus fiction.
2. Generate preview.
3. Confirm the subject routes to **Fiction** when the non-fiction rule disables fiction.

Expected result:
- fiction-protected categories do not claim fiction subjects

### Case 5: Juvenile exclusion case
1. Load a subject set containing a juvenile-prefixed subject that also contains a normal category keyword.
2. Generate preview.

Expected result:
- the normal category does not claim the subject when juvenile exclusion applies

### Case 6: Multiple category case
1. Load a subject such as one that should match more than one category.
2. Generate preview.

Expected result:
- the subject appears in multiple category groups when multi-match is enabled

### Case 7: Uncategorized case
1. Load a subject that does not fit any configured category.
2. Generate preview.

Expected result:
- the subject appears in the uncategorized preview
- the uncategorized reason is visible

### Case 8: Export file verification
1. Load the dummy CSV.
2. Complete review.
3. Generate preview.
4. Choose an output folder explicitly.
5. Export.

Expected result:
- category files are created
- `NotCategorizedSubjects.txt` is created
- `RunSummary.txt` is created
- output folder opens successfully
- output paths shown in the app match the actual files

### Case 9: Output folder selection requirement
1. Load input and generate preview.
2. Do not choose an output folder.
3. Attempt export.

Expected result:
- export does not proceed
- the app shows a friendly message asking the user to choose an output folder first

### Case 10: summary counts versus exported file line counts
1. Load input and complete export.
2. Open `RunSummary.txt`.
3. Compare the assignment metrics with the actual exported category file line counts.

Expected result:
- assignment counts are present
- exported unique line counts are present
- a note explaining the difference is present
- counts reconcile correctly

---

## 4. Local-only exploratory validation with the original library CSV

The original full library CSV should remain local and should not be committed to the repository.

Use it only for exploratory validation:

- verify extraction succeeds
- verify preview succeeds
- inspect high-frequency uncategorized subjects
- document rule coverage gaps
- do not add new subject lists in Phase 15
- do not introduce a second categorization mode in Phase 15

If significant accuracy gaps remain after validation, create a later focused branch for conservative rule tuning.

---

## 5. Release Confidence Checklist

Before release, confirm:

- automated tests all pass
- dummy fixture benchmark passes
- manual CSV validation passes
- manual plain text validation passes
- output folder must be chosen by the user
- export succeeds to a user-selected folder
- log folder opens correctly
- run summary is generated correctly

---

## 6. Notes

Phase 15 keeps Bragi in a single legacy-compatible categorization mode.

Any future modernization of rule behavior should happen only after Phase 15 results clearly justify it.
