# Failed xUnit capture sample

This deliberately failing, dependency-free payment retry fixture demonstrates Milestone 1. It is outside PulseGrid.sln so product tests stay green. The complete API/PostgreSQL/container payment demonstration belongs to later milestones.

```sh
dotnet restore samples/FailedTests/FailedTests.csproj --configfile NuGet.Config
dotnet test samples/FailedTests/FailedTests.csproj --no-restore --logger "trx;LogFileName=failed.trx" --results-directory artifacts/sample-results
dotnet run --project src/Cli -- capture --source artifacts/sample-results/failed.trx --output artifacts/failure.pgbundle --json
dotnet run --project src/Cli -- inspect artifacts/failure.pgbundle
dotnet run --project src/Cli -- validate artifacts/failure.pgbundle --json
```

The sample test command returns 1 by design; capture returns 0 with execution status failed. The failure proves duplicate capture on retry. Bundle diagnostics and display parameters are excluded.
