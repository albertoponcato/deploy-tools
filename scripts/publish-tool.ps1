<#
Packs Exetera.DeployTools and pushes it to the NuGet feed in one step, reading the version
to publish from Directory.Build.props (PackageVersion) instead of a wildcard match, so a
stale .nupkg from a previous version can never get pushed by mistake.

Usage:
  .\publish-tool.ps1
  .\publish-tool.ps1 -ApiKey <key>
  .\publish-tool.ps1 -Source "https://gitlab.exetera.com/api/v4/projects/<id>/packages/nuget/index.json"
#>
param(
    [string]$ApiKey,
    [string]$Source = "gitlab.exetera.com"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot "DeployTools.csproj"
$buildProps = Join-Path $repoRoot "Directory.Build.props"

[xml]$props = Get-Content $buildProps
$version = $props.Project.PropertyGroup.PackageVersion
if (-not $version) {
    throw "Could not read PackageVersion from $buildProps"
}

[xml]$csprojXml = Get-Content $csproj
$packageId = $csprojXml.Project.PropertyGroup.PackageId | Where-Object { $_ } | Select-Object -First 1
if (-not $packageId) {
    throw "Could not read PackageId from $csproj"
}

Write-Host "Packing $packageId $version..."
dotnet pack $csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

$nupkg = Join-Path $repoRoot "bin\Release\$packageId.$version.nupkg"
if (-not (Test-Path $nupkg)) {
    throw "Expected package not found after pack: $nupkg"
}

$pushArgs = @($nupkg, "--source", $Source)
if ($ApiKey) { $pushArgs += @("--api-key", $ApiKey) }

Write-Host "Pushing $nupkg to $Source..."
dotnet nuget push @pushArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet nuget push failed" }

Write-Host "Published $version to $Source"
