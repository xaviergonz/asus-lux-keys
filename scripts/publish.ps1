param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "publish",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
. $PSScriptRoot/config.ps1

dotnet publish $AppProjectFile `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained $SelfContained.IsPresent.ToString().ToLowerInvariant() `
    --output $Output

if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
