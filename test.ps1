# Build the Windows test project and run its xUnit suite with dotnet test.
# Additional arguments are forwarded to dotnet test (for example --filter FullyQualifiedName~MouseCaptureTests).
param(
  [ValidateSet('Debug', 'Release')]
  [string]$Configuration = 'Debug',
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$TestArgs
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'GestureSign.Tests\GestureSign.Tests.csproj'

dotnet test $project -c $Configuration @TestArgs
exit $LASTEXITCODE
