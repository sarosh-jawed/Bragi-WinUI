# Known Limitations

This document records the current, expected limitations of Bragi at the current release-readiness stage.

## 1. Conservative keyword-driven categorization

Bragi currently categorizes subjects using configuration-driven keyword matching.

This is intentional and acceptable for the current project scope, but it means:

- not every subject will be categorized
- uncategorized output is expected
- some future refinement may later use Library of Congress-driven or other controlled-library reference logic

## 2. Uncategorized subjects are expected

`NotCategorizedSubjects.txt` is a normal output artifact.

It should not be interpreted as a failure by itself. It simply contains subjects that did not match the currently configured category rules.

## 3. Export files currently contain subject text only

The current export format writes subject text lines only.

The app can capture additional source metadata internally, but the category files themselves currently export plain text subjects because that is the confirmed desired output format at this stage.

## 4. User must choose an output folder

The app does not currently auto-export to a silent default UI folder.

This is intentional. The user must explicitly choose the output folder before export.

## 5. Public MSIX packaging is deferred

The project is ready for release preparation, but the final public MSIX package should only be generated once:

- publisher identity is finalized
- signing approach is finalized
- the final release cut is approved

## 6. Current release target is desktop x64-first

The repository is prepared around the current x64 desktop workflow. Additional release targets can be expanded later if needed.

## 7. Future classification refinement is outside current scope

Later refinement may map or validate subjects against more formal library-controlled references. That work is intentionally not part of the current release-readiness phase.