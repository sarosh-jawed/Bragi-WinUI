# Bragi v1.0.0 Release Notes

## Summary

Bragi v1.0.0 is the first release-ready WinUI 3 rebuild of the legacy Bragi subject categorization workflow.

This release modernizes the desktop experience while preserving the current confirmed output model:

- one file per configured subject category
- `NotCategorizedSubjects.txt`
- `RunSummary.txt`

## Highlights

- modern WinUI 3 desktop shell
- guided five-step workflow
- plain text and CSV input support
- configurable CSV extraction from `instance.subjects`
- review and preview workflow before export
- structured logging and friendly error handling
- conservative configuration-driven categorization
- automated regression coverage and release-readiness documentation

## Included Output Model

Current release behavior exports:

- one text file per enabled category
- subject lines only in category files
- uncategorized subject output in `NotCategorizedSubjects.txt`
- run-level metrics in `RunSummary.txt`

## Validation

This release candidate has been validated through:

- automated xUnit coverage
- regression fixture validation
- manual CSV validation
- manual plain text validation
- real-file local exploratory validation against the original library CSV

## Known limitations

See `docs/KNOWN-LIMITATIONS.md`.

## Notes

Later refinement may expand categorization quality using more formal library-controlled reference logic, but that work is outside the scope of this release-ready baseline.