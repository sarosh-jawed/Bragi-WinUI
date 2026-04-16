# Changelog

All notable changes to this project will be documented in this file.

This project follows a simple, human-readable changelog style inspired by Keep a Changelog.

## [Unreleased]
### Changed
- hardened operational logging across startup, extraction, categorization, and export
- improved cancellation handling for long-running workflow operations
- replaced raw exception text in the UI with friendly user-facing messages
- expanded `config.json` to include the full legacy Bragi subject category rule set in preparation for Phase 15 validation
- required the user to choose an output folder before export instead of relying on a default UI output path
- added Phase 15 automated validation coverage and manual validation documentation
- conservatively tuned legacy category keywords to improve coverage for the original library CSV without adding new categories or introducing a second categorization mode

### Added
- Phase 1 repository baseline and project standards
- professional README structure
- changelog baseline
- shared editor configuration
- project conventions document
- testing baseline document
- screenshots tracking document

## [0.1.0] - 2026-04-07

### Added
- public GitHub repository created
- initial commit with `README.md`, `LICENSE`, and `.gitignore`
- clean reset established for the modernized Bragi codebase
