# PulseGrid

Engineering intelligence for software teams. PulseGrid captures execution evidence locally in portable, validated bundles.

This repository implements **Milestone 1 — Evidence MVP**, using .NET 10 LTS / C# 14. The product baseline is [PULSEGRID_SPEC.md](PULSEGRID_SPEC.md). See [implementation status](docs/implementation-status.md) and [architecture decisions](docs/architecture/0001-evidence-mvp.md).

## Build and test

Install .NET SDK 10.0.404, then:

```sh
dotnet restore PulseGrid.sln --configfile NuGet.Config
dotnet build PulseGrid.sln -c Release --no-restore
dotnet test PulseGrid.sln -c Release --no-build
dotnet pack PulseGrid.sln -c Release --no-build --output artifacts/packages
```

## Capture and inspect

Run an existing xUnit test project with VSTest's TRX logger:

```sh
dotnet test path/to/Your.Tests.csproj --logger "trx;LogFileName=results.trx" --results-directory artifacts/test-results
dotnet run --project src/Cli -- capture --source artifacts/test-results/results.trx --output artifacts/run.pgbundle --json
dotnet run --project src/Cli -- inspect artifacts/run.pgbundle
dotnet run --project src/Cli -- validate artifacts/run.pgbundle --json
dotnet run --project src/Cli -- export artifacts/run.pgbundle --output artifacts/exported.pgbundle
dotnet run --project src/Cli -- import artifacts/exported.pgbundle --output artifacts/imported.pgbundle
dotnet run --project src/Cli -- list artifacts --json
```

Choose a new output path for each command; existing files are never overwritten. Capture accepts one report, or a directory containing exactly one top-level report after an optional `--exclude <filename-pattern>`. Add `--revision <revision>` and `--branch <branch>` only for metadata you intentionally want to include. Environment variables and Git metadata are not automatically collected.

The [failed xUnit sample](samples/FailedTests/README.md) is an executable first-use workflow. Install the packed CLI with the instructions in [src/Cli/README.md](src/Cli/README.md).

## Modules

- `Core`: validated execution models, with no external dependencies.
- `Specifications`: versioned JSON contracts and JSON Schema validation.
- `Abstractions`: adapter/storage extension contracts.
- `Evidence`: capture orchestration.
- `Evidence.Storage.Local`: atomic ZIP storage and validated streaming.
- `Evidence.Xunit`: sanitized TRX ingestion.
- `Cli`: local command composition.

No server, database, hosted account or AI provider is required. Raw TRX, diagnostic messages, stack traces, test output and parameterized display names are excluded. Inspect retained identifiers before sharing evidence. Hash validation detects modifications relative to the bundle's recorded digests; it does not authenticate the producer.

Read the [bundle guide](docs/guides/evidence-bundles.md), [security policy](SECURITY.md), [contribution guide](CONTRIBUTING.md) and [branch checks](docs/contributors/branch-protection.md).
