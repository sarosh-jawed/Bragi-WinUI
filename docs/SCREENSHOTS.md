# Bragi Screenshot Tracker

This document tracks the release-quality screenshots that should exist before the final packaging and GitHub release cut.

---

## Required Release Screenshots

### Application Shell
- main shell window
- sidebar navigation
- footer navigation state

### Workflow Pages
- start page
- load input page
- review subjects page
- review completed state
- preview results page
- preview page with clarified counts if visible
- export and finish page
- export page with generated files
- final run summary snippet if relevant

### Operational States
- successful export confirmation
- generated output folder example
- log folder example

Busy/loading state is optional and may be omitted if it is difficult to capture cleanly.

### Repository / Release
- polished GitHub repository home page
- release page once v1.0.0 is cut

---

## Naming Convention

Use stable lowercase names with hyphen separators.

Recommended names:

- `shell-main-window.png`
- `start-page.png`
- `load-input-page.png`
- `review-subjects-page.png`
- `review-completed-state.png`
- `preview-results-page.png`
- `preview-clarified-counts.png`
- `export-finish-page.png`
- `export-generated-files.png`
- `run-summary-snippet.png`
- `successful-export.png`
- `output-folder.png`
- `log-folder.png`
- `github-home.png`
- `github-release.png`

---

## Storage Location

Store screenshots in:

- `docs/screenshots/`

---

## Current Status

Application workflow and QA screenshots have now been captured in `docs/screenshots/` for the final pre-packaging validation pass.

Still deferred until final release publication:
- polished GitHub repository home page screenshot
- GitHub release page screenshot once `v1.0.0` is cut
