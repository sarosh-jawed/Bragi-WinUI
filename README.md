# Bragi-WinUI

Modern WinUI 3 desktop application for configurable library subject categorization and legacy-compatible subject list generation.

## Overview

Bragi-WinUI is the modernized desktop version of Bragi.

The application will preserve the practical behavior of the legacy Bragi tool while replacing the old hard-coded Windows Forms implementation with a cleaner, more maintainable WinUI 3 architecture.

Bragi is being rebuilt as a professional desktop application with:
- configuration-driven categorization rules
- clearer workflow and review steps
- structured logging
- predictable export behavior
- testable service boundaries
- clean project architecture aligned with Hel standards

## What Bragi Does

Bragi processes subject data and routes each extracted subject into discipline-specific output files.

The modernized application is intended to:
- load a plain text file with one subject per line
- load a CSV file and extract subjects from a configured column such as `instance.subjects`
- normalize subject values safely
- categorize subjects using configuration, not hard-coded processor logic
- preserve legacy-compatible output file names
- generate uncategorized output and a run summary
- provide review, preview, and export steps in a guided desktop workflow

## Planned Inputs

Bragi will support these input types:

1. Plain text
   - one subject per line

2. CSV
   - subjects extracted from a configured column
   - default legacy-friendly support for `instance.subjects`
   - support for subject arrays stored as text inside CSV cells

## Planned Outputs

Bragi will generate:
- one text file per configured category
- `NotCategorizedSubjects.txt`
- `RunSummary.txt`

Operationally, the application is also expected to support:
- output folder opening
- log folder opening
- review and preview before export

## Planned Architecture

This repository follows a clean multi-project desktop architecture.

Planned solution and project names:
- `Bragi`
- `Bragi.Domain`
- `Bragi.Application`
- `Bragi.Infrastructure`
- `Bragi.App.WinUI`
- `Bragi.Tests`

Planned responsibility split:
- `Bragi.Domain` - core records, enums, value objects, run results
- `Bragi.Application` - contracts, configuration models, workflow state
- `Bragi.Infrastructure` - ingest, extraction, categorization, export, config, logging helpers
- `Bragi.App.WinUI` - WinUI 3 desktop shell and workflow pages
- `Bragi.Tests` - xUnit tests, fixtures, regression coverage

## Repository Status

Current status:
- Phase 0 completed
- Phase 1 baseline and standards in progress
- implementation code has intentionally not started yet

Important rule:
This repository is a clean reset. The old Bragi repository is used only as:
- behavior reference
- comparison point
- regression reference

## Planned Operational Defaults

Default operational paths are expected to follow this pattern:
- logs: `%LOCALAPPDATA%\Bragi\Logs`
- default outputs: `%LOCALAPPDATA%\Bragi\Output\YYYY-MM`

Configuration loading is planned to support:
- packaged `config.json`
- optional local override at `%LOCALAPPDATA%\Bragi\config.local.json`

## Release and Install Notes

Release packaging will be added in later phases.

Planned release artifacts:
- MSIX package
- ZIP release bundle
- checksums
- install notes
- example local configuration file

This section will be updated once build, packaging, and release automation are implemented.

## Development Principles

Before coding starts, this repository follows these standards:
- no core business logic in UI code-behind
- no hard-coded category rules in processor classes
- no reverse project references
- no direct file I/O from UI pages
- main branch stays stable
- work is performed in phase-based feature branches

## License

This repository uses the MIT License.
