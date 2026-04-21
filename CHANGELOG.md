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
- improved initial input-load responsiveness for large CSV files by moving heavy CSV read and extraction work off the UI thread and showing explicit load progress feedback
- added conservative high-confidence keyword coverage improvements for Computer, Math, Humanities, and SLIM based on validation against the original library CSV
- added test coverage for conservative keyword coverage improvements in Computer, Math, Humanities, and SLIM
- polished README to reflect the actual implemented Bragi workflow and release-readiness state
- updated screenshot tracking for final release preparation
- documented future merge-readiness and Hel-aligned architectural checkpoints

### Added
- release-readiness documentation for install notes, known limitations, release notes, and architecture audit
- release bundle preparation script for final packaging
- GitHub Actions CI workflow for restore, build, and test
- example local configuration override file
- folder publish profile for `win-x64`

## [0.1.0] - 2026-04-07

### Added
- public GitHub repository created
- initial commit with `README.md`, `LICENSE`, and `.gitignore`
- clean reset established for the modernized Bragi codebase
