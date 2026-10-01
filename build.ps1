# Build the .NET 10 Windows desktop solution.
param(
  [ValidateSet('Debug', 'Release', 'Portable', 'Centennial', 'uiAccessRelease')]
  [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'GestureSign.sln'

dotnet restore $solution -p:Configuration=$Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution --no-restore -c $Configuration
exit $LASTEXITCODE
