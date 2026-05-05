param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
. $PSScriptRoot/config.ps1

$arguments = @(
    "build",
    $SolutionFile,
    "--configuration",
    $Configuration,
    "-m:1"
)

dotnet @arguments
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}
