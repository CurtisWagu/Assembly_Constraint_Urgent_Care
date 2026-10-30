<#
.SYNOPSIS
  Builds a Release version of Assembly Constraint Urgent Care and packages the add-in bundle as a zip
  that can be attached to a GitHub release.

.DESCRIPTION
  Output: release\Assembly_Constraint_Urgent_Care.bundle.zip
  The zip contains one folder, Assembly_Constraint_Urgent_Care.bundle, with PackageContents.xml at the top
  and the add-in files inside a Contents folder. This is the layout the README describes.

  Run from the project folder (right-click the file > Run with PowerShell, or run it in a terminal):
      .\Make-Release.ps1

  Use -SkipBuild to package an existing Release build without building again.
  The build does not install the add-in on this PC, so it works even while Inventor is open.
#>
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$project    = Join-Path $root 'Assembly_Constraint_Urgent_Care.vbproj'
$outDir     = Join-Path $root 'bin\Release'
$releaseDir = Join-Path $root 'release'
$bundleName = 'Assembly_Constraint_Urgent_Care.bundle'
$zipPath    = Join-Path $releaseDir 'Assembly_Constraint_Urgent_Care.bundle.zip'

if (-not $SkipBuild) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet was not found. Build the project in Visual Studio with the Release configuration, then run this script again with -SkipBuild.'
    }
    Write-Host 'Building the Release version...'
    & dotnet build $project -c Release -p:SkipAddinInstall=true
    if ($LASTEXITCODE -ne 0) { throw 'The build failed. Fix the errors above and try again.' }
}

if (-not (Test-Path (Join-Path $outDir 'Assembly_Constraint_Urgent_Care.dll'))) {
    throw "Assembly_Constraint_Urgent_Care.dll was not found in $outDir. Build the Release configuration first."
}

# Lay the bundle out in a temporary folder.
$stage    = Join-Path ([System.IO.Path]::GetTempPath()) ('UrgentCareRelease_' + [guid]::NewGuid().ToString('N'))
$bundle   = Join-Path $stage $bundleName
$contents = Join-Path $bundle 'Contents'
New-Item -ItemType Directory -Path $contents -Force | Out-Null

Copy-Item (Join-Path $outDir 'PackageContents.xml') $bundle

Get-ChildItem $outDir -Recurse -File |
    Where-Object { $_.Name -ne 'PackageContents.xml' -and $_.Extension -ne '.pdb' } |
    ForEach-Object {
        $relative = $_.FullName.Substring($outDir.Length).TrimStart('\')
        $target   = Join-Path $contents $relative
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item $_.FullName $target
    }

# Zip it, with the .bundle folder as the top level.
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath }

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($bundle, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $true)
Remove-Item $stage -Recurse -Force

Write-Host ''
Write-Host "Created: $zipPath"
Write-Host 'Contents of the zip:'
$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try { $zip.Entries | ForEach-Object { Write-Host ('  ' + $_.FullName) } } finally { $zip.Dispose() }
Write-Host ''
Write-Host 'Next: on GitHub go to Releases > Create a new release, and attach this zip.'
