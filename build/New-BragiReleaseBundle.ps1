param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$PublishedFolder,

    [Parameter(Mandatory = $false)]
    [string]$MsixPath,

    [Parameter(Mandatory = $false)]
    [string]$OutputRoot = "artifacts\release",

    [Parameter(Mandatory = $false)]
    [string]$ReleaseNotesPath
)

$ErrorActionPreference = "Stop"

function New-CleanDirectory {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }

    New-Item -ItemType Directory -Path $Path | Out-Null
}

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Label
    )

    if (-not (Test-Path $Path)) {
        throw "$Label was not found: $Path"
    }
}

Write-Host "Preparing Bragi release bundle for version $Version..."

Assert-PathExists -Path $PublishedFolder -Label "Published folder"

if ([string]::IsNullOrWhiteSpace($ReleaseNotesPath)) {
    $ReleaseNotesPath = "docs\RELEASE-NOTES-v$Version.md"
}

$releaseRoot = Resolve-Path "."
$bundleRoot = Join-Path $releaseRoot $OutputRoot
New-CleanDirectory -Path $bundleRoot

$zipName = "Bragi-v$Version-win-x64.zip"
$zipPath = Join-Path $bundleRoot $zipName

Write-Host "Creating ZIP bundle..."
Compress-Archive -Path (Join-Path $PublishedFolder "*") -DestinationPath $zipPath -Force

if (-not [string]::IsNullOrWhiteSpace($MsixPath)) {
    Assert-PathExists -Path $MsixPath -Label "MSIX package"

    $msixName = "Bragi-v$Version-x64.msix"
    $targetMsixPath = Join-Path $bundleRoot $msixName

    Write-Host "Copying MSIX package..."
    Copy-Item -Path $MsixPath -Destination $targetMsixPath -Force
}
else {
    Write-Host "MSIX package was not supplied. ZIP-only bundle will be prepared for now."
}

Write-Host "Copying supporting release files..."
Copy-Item -Path "config.local.example.json" -Destination (Join-Path $bundleRoot "config.local.example.json") -Force
Copy-Item -Path "docs\INSTALL.md" -Destination (Join-Path $bundleRoot "INSTALL.md") -Force
Assert-PathExists -Path $ReleaseNotesPath -Label "Release notes file"
Copy-Item -Path $ReleaseNotesPath -Destination (Join-Path $bundleRoot ("RELEASE-NOTES-v{0}.md" -f $Version)) -Force

$hashFile = Join-Path $bundleRoot "SHA256SUMS.txt"
$artifacts = Get-ChildItem -Path $bundleRoot -File | Sort-Object Name

$hashLines = foreach ($artifact in $artifacts) {
    $hash = Get-FileHash -Path $artifact.FullName -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $artifact.Name
}

Set-Content -Path $hashFile -Value $hashLines -Encoding UTF8

Write-Host ""
Write-Host "Release bundle created:"
Get-ChildItem -Path $bundleRoot -File | Sort-Object Name | ForEach-Object {
    Write-Host " - $($_.Name)"
}
