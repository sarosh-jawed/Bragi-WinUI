# Bragi-WinUI

Modern WinUI 3 desktop application for configurable library subject categorization and legacy-compatible subject list generation.

## Overview

Bragi-WinUI is the modern desktop rebuild of the legacy Bragi tool.

The application preserves the practical workflow of the original tool while replacing the old hard-coded implementation with a cleaner WinUI 3 architecture that is easier to maintain, test, and release.

Bragi now supports:

- configuration-driven categorization rules
- plain text and CSV input
- guided review, preview, and export workflow
- structured logging
- friendly user-facing error handling
- deterministic export behavior
- automated regression coverage
- release-readiness documentation and CI scaffolding

## What Bragi Does

Bragi processes extracted subject data and routes subjects into discipline-specific output files.

The application can:

- load a plain text file with one subject per line
- load a CSV file and extract subjects from a configured column such as `instance.subjects`
- normalize and deduplicate extracted subjects
- categorize subjects using configuration rather than hard-coded processor logic
- generate one text file per configured category
- generate `NotCategorizedSubjects.txt`
- generate `RunSummary.txt`
- provide review and preview steps before export
- export to a user-selected output folder

## Current Input Model

Default CSV configuration:

- subject column: `instance.subjects`
- title column: `instance.title`
- record ID column: `instance.id`

The app can capture title and record ID metadata internally for preview and troubleshooting, while the exported category files currently write subject text only.

## Current Output Model

Bragi currently produces:

- one text file per enabled category
- `NotCategorizedSubjects.txt`
- `RunSummary.txt`

Current category output behavior:

- output lines contain the original extracted subject text
- output is sorted
- output is deduplicated
- uncategorized subjects are preserved in a separate file

This output format has been confirmed as acceptable for the current project stage.

RunSummary.txt reports categorization assignment counts. Exported category files are sorted and deduplicated unique subject outputs, so file line counts may be lower than assignment counts.

## Workflow

Bragi uses a guided five-step workflow:

1. **Start**
2. **Load Input**
3. **Review Subjects**
4. **Preview Results**
5. **Export & Finish**

## Solution Structure

The solution follows a layered desktop architecture:

- `Bragi.Domain`
- `Bragi.Application`
- `Bragi.Infrastructure`
- `Bragi.App.WinUI`
- `Bragi.Tests`

Responsibility split:

- `Bragi.Domain` - core records, enums, value objects, result models
- `Bragi.Application` - contracts, config models, workflow state, user-facing abstractions
- `Bragi.Infrastructure` - config loading, input ingest, extraction, categorization, export, path resolution
- `Bragi.App.WinUI` - shell, pages, view models, navigation, user interaction
- `Bragi.Tests` - xUnit automated coverage and regression tests

## Current Status

Implemented through Phase 15:

- guided WinUI 3 shell and workflow pages
- CSV and plain text input ingestion
- subject extraction
- configuration-driven categorization
- export generation
- structured logging
- cancellation-safe workflow execution
- automated regression coverage
- conservative keyword coverage improvements validated against the original library CSV
- preview and review performance improvements for larger CSV files

## Build and Test

From repo root:

```powershell
dotnet restore Bragi/Bragi.sln
dotnet build Bragi/Bragi.sln -c Debug -p:Platform=x64
dotnet test Bragi/Bragi.Tests/Bragi.Tests.csproj -c Debug
