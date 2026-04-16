# Bragi-WinUI

Modern WinUI 3 desktop application for configurable library subject categorization and legacy-compatible subject list generation.

## Overview

Bragi-WinUI is the modernized desktop version of Bragi.

The application preserves the practical behavior of the legacy Bragi tool while replacing the old hard-coded Windows Forms implementation with a cleaner, more maintainable WinUI 3 architecture.

## Current Implementation Status

Phases 0 through 13 are now represented in the codebase.

The current repository already includes:

- a .NET 8 multi-project solution
- a packaged WinUI 3 desktop shell
- dependency injection and hosted startup
- packaged and local configuration loading
- configuration validation
- plain text and CSV input ingestion
- subject extraction from configured CSV columns
- JSON-array-style subject extraction support for CSV cells such as `instance.subjects`
- configuration-driven categorization
- guided 5-step shell navigation
- preview and export workflow
- run summary generation
- structured file logging
- automated unit tests for extraction, categorization, export, and workflow state

## Current Workflow

The implemented shell currently uses this guided workflow:

1. Start
2. Load Input
3. Review Subjects
4. Preview Results
5. Export & Finish

## Current Architecture

This repository follows a clean multi-project desktop architecture.

Projects:

- `Bragi.Domain`
- `Bragi.Application`
- `Bragi.Infrastructure`
- `Bragi.App.WinUI`
- `Bragi.Tests`

Responsibility split:

- `Bragi.Domain` - core records, enums, value objects, and run results
- `Bragi.Application` - contracts, configuration models, and workflow/session state
- `Bragi.Infrastructure` - ingest, extraction, categorization, export, workflow services, and configuration helpers
- `Bragi.App.WinUI` - packaged WinUI 3 shell, pages, startup, and view models
- `Bragi.Tests` - xUnit coverage for core services and workflow behavior

## Current Input Support

Bragi currently supports these input types:

1. Plain text
   - one subject per line

2. CSV
   - subjects extracted from a configured column
   - default support for `instance.subjects`
   - support for JSON-style subject arrays stored as text in CSV cells

## Current Output Support

Bragi currently generates:

- one text file per enabled category rule
- `NotCategorizedSubjects.txt`
- `RunSummary.txt`

The application also supports:

- choosing an output folder
- opening the output folder after export
- opening the log folder
- review and preview before export

## Current Configuration Status

Configuration is loaded from:

- packaged `config.json`
- optional `%LOCALAPPDATA%\Bragi\config.local.json`

Current operational defaults:

- logs: `%LOCALAPPDATA%\Bragi\Logs`
- default outputs: `%LOCALAPPDATA%\Bragi\Output\YYYY-MM`

The packaged `config.json` currently ships with a starter category set:

- Art
- Biology
- Business
- Chemistry

That is not yet the full legacy-compatible category surface. Full rule completion should be handled in a separate functional branch so it does not get mixed into logging hardening or release work.

## Current Automated Coverage

The repository already includes automated tests for:

- subject extraction
- categorization
- text export
- workflow state/session behavior
- workflow orchestration
- step navigation behavior

## Repository Role

This repository is the active modern Bragi implementation.

The old Bragi repository remains useful as:

- behavior reference
- comparison point
- regression reference

## Release Status

Release packaging is not finalized yet.

Later phases will complete:

- release bundle outputs
- publish profiles
- checksums
- install notes
- GitHub release readiness

## Development Principles

This repository continues to follow these rules:

- no core business logic in UI code-behind
- no hard-coded category rules in processor classes
- no reverse project references
- no direct file I/O from workflow pages
- main branch stays stable
- focused feature branches for scoped work

## License

This repository uses the MIT License.
