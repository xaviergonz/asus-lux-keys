param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
. $PSScriptRoot/config.ps1

$arguments = @(
    "test",
    $TestProjectFile,
    "--configuration",
    $Configuration
)

dotnet @arguments
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
