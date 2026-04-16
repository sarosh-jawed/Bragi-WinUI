# Changelog

All notable changes to this project will be documented in this file.

This project follows a simple, human-readable changelog style inspired by Keep a Changelog.

## [Unreleased]

### Added
- .NET 8 multi-project Bragi solution
- `Bragi.Domain`, `Bragi.Application`, `Bragi.Infrastructure`, `Bragi.App.WinUI`, and `Bragi.Tests`
- WinUI 3 packaged desktop shell with 5-step workflow
- hosted startup with dependency injection and configuration loading
- configuration normalization and validation
- plain text input ingestion
- CSV ingestion with configurable subject-column extraction
- subject extraction with JSON-array CSV cell support
- configuration-driven categorization services
- export generation for category files, uncategorized output, and run summary
- workflow orchestrator, session store, and step navigation services
- automated tests for extraction, categorization, export, and workflow behavior

### Changed
- synchronized `README.md` to the real Phase 13 implementation state
- synchronized `docs/TEST-CASES.md` to the real Phase 13 implementation state
- aligned `WizardState.CreateInitial()` with the 5-step shell model

## [0.1.0] - 2026-04-07

### Added
- public GitHub repository created
- initial commit with `README.md`, `LICENSE`, and `.gitignore`
- clean reset established for the modernized Bragi codebase
