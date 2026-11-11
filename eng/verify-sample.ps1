$ErrorActionPreference = 'Stop'

dotnet restore samples/FailedTests/FailedTests.csproj --configfile NuGet.Config
if ($LASTEXITCODE -ne 0) { throw 'Sample restore failed.' }
dotnet test samples/FailedTests/FailedTests.csproj --no-restore --logger 'trx;LogFileName=failed.trx' --results-directory artifacts/sample-results
if ($LASTEXITCODE -ne 1) { throw 'The demonstration test must fail with exit code 1.' }
if (!(Test-Path -LiteralPath 'artifacts/sample-results/failed.trx')) { throw 'The sample did not produce a TRX report.' }

$taskBundle = Join-Path 'artifacts' ('sample-' + [Guid]::NewGuid().ToString('N') + '.pgbundle')
$taskCapture = dotnet src/Cli/bin/Release/net10.0/PulseGrid.Cli.dll capture --source artifacts/sample-results/failed.trx --output $taskBundle --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $taskCapture.execution.status -ne 'failed') { throw 'Sample capture did not retain the failed execution.' }
$taskValidation = dotnet src/Cli/bin/Release/net10.0/PulseGrid.Cli.dll validate $taskBundle --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or !$taskValidation.valid) { throw 'Sample bundle validation failed.' }
$taskInspection = dotnet src/Cli/bin/Release/net10.0/PulseGrid.Cli.dll inspect $taskBundle --json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $taskInspection.tests.Count -ne 1 -or $taskInspection.tests[0].outcome -ne 'failed') { throw 'Sample inspection did not retain the failed test.' }
if ($taskInspection.tests[0].diagnostics -ne '[REDACTED]') { throw 'Sample diagnostic exclusion failed.' }
Write-Output 'Real xUnit failure captured, validated and inspected.'
