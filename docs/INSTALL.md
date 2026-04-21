# Bragi Install Notes

These notes describe the intended installation and first-run behavior for Bragi release builds.

## Supported Environment

- Windows 10 or Windows 11
- x64 desktop environment
- local user access to choose input and output folders

## Planned Release Artifacts

Final release packaging is expected to provide:

- `Bragi-v1.0.0-x64.msix`
- `Bragi-v1.0.0-win-x64.zip`
- `SHA256SUMS.txt`
- `config.local.example.json`

## ZIP Install

1. Download the ZIP release bundle.
2. Extract it to a local folder.
3. Launch `Bragi.App.WinUI.exe`.
4. Use the guided workflow:
   - Start
   - Load Input
   - Review Subjects
   - Preview Results
   - Export & Finish
5. Choose an output folder explicitly before export.

## MSIX Install

The public MSIX install flow will be finalized at the final release cut once package identity and signing details are locked.

Expected process:

1. Download the MSIX package.
2. Install the package.
3. Launch Bragi from the Windows app list.
4. Run the normal workflow.

## First-Run Notes

Current operational behavior:

- the app reads packaged `config.json`
- local configuration override may later be supported via `%LOCALAPPDATA%\Bragi\config.local.json`
- Logs are written to %DOCUMENTS%\Bragi\Logs.
- run summary reporting includes assignment metrics and final exported file metrics
- category files currently contain subject text only
- the user must explicitly choose an output folder before export

## Troubleshooting

If Bragi cannot complete export:

- verify the selected output folder exists and is writable
- verify the CSV input file is readable
- check the log folder for technical details
- confirm the CSV structure matches the configured subject column expectations
- assignment counts and exported file line counts may differ because exported category files are sorted and deduplicated

## Current Status

These install notes are release-ready documentation. The final public packaging run is intentionally deferred until the last release step.
