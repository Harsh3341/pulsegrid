# PulseGrid

[![CI](https://github.com/Harsh3341/pulsegrid/actions/workflows/ci.yml/badge.svg?branch=development)](https://github.com/Harsh3341/pulsegrid/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Engineering intelligence for software teams. PulseGrid captures execution evidence locally in portable, validated bundles.

This repository implements **Milestone 1 — Evidence MVP**, using .NET 10 LTS / C# 14. It is an early development project; packages are built locally and have not been published. See [implementation status](docs/implementation-status.md) and [architecture decisions](docs/architecture/0001-evidence-mvp.md).

## What you can do today

- Capture an existing xUnit TRX report as sanitized execution evidence.
- Inspect test outcomes through human-readable or JSON CLI output.
- Validate versioned schemas, ZIP entry paths and SHA-256 integrity hashes.
- Import, export and move bundles without retaining the original report.

The [failed xUnit sample](samples/FailedTests/README.md) walks through capture, inspection and validation using a real failing test.

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

## Contributing and support

Use [issues](https://github.com/Harsh3341/pulsegrid/issues) for reproducible bugs, feature proposals and usage questions. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request; changes target `development` and require review plus both CI checks.

Report vulnerabilities through [private security reporting](https://github.com/Harsh3341/pulsegrid/security/advisories/new). Keep credentials, raw reports and private execution evidence out of public issues.

PulseGrid is [MIT licensed](LICENSE).
