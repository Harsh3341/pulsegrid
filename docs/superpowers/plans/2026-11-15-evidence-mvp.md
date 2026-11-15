# Evidence MVP Implementation Plan

**Goal:** Implement roadmap Milestone 1 and the foundation required to build it.
**Architecture:** Local-first modular application with inward project dependencies. ZIP bundles carry versioned JSON and SHA-256 content references. An adapter ingests explicitly selected xUnit TRX results without executing commands.
**Tech stack:** .NET 10 LTS / C# 14, System.Text.Json, System.IO.Compression, System.Xml, xUnit for tests.
**Spec:** [PULSEGRID_SPEC.md](../../../PULSEGRID_SPEC.md), especially sections 5–9, 14–17.

The user authorized proposing this plan and then implementing it in this session. Git was subsequently initialized on the requested development branch, with focused commits and origin https://github.com/Harsh3341/pulsegrid.git. Plain commit messages describe changes without attribution.

## Constraints

- No mandatory server, cloud, database, AI, or container dependency.
- No empty future modules. Nullable types, documented public contracts, central packages, deterministic builds.
- Secrets redacted or excluded by default. Never copy raw results, source files, environment variables, or test output into bundles.
- Missing/incomplete evidence cannot silently become a pass.

## Tasks and file map

- [x] Foundation and model: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.Config`, `PulseGrid.sln`, `src/Core/Models.cs`, `src/Specifications/BundleManifest.cs`, `ManifestCodec.cs`, `specifications/evidence-bundle/1.0.0.schema.json`, example manifest; `tests/Unit/ModelTests.cs`. Validate empty identifiers, UTC timestamps, incomplete execution and schema compatibility.
- [x] Capture: `src/Abstractions/Contracts.cs`, `src/Evidence/CaptureService.cs`, `adapters/Xunit/TrxEvidenceAdapter.cs`; unit tests with hand-written TRX fixtures. Stream XML with DTD disabled, capture stable method identifiers only, omit diagnostic contents, classify missing/unknown outcomes as inconclusive.
- [x] Local bundle storage: `src/Evidence/Storage.Local/LocalBundleStore.cs` and focused archive validation helpers; `tests/Integration/BundleTests.cs`. Test round-trip, artifact and manifest tampering, duplicate/unlisted/unsafe entries, oversized data, cancelled/interrupted creation, overwrite prevention, validated copy and extraction.
- [x] CLI: `src/Cli/Program.cs`, `CliApplication.cs`, argument parsing; `tests/Integration/CliTests.cs`. Implement capture/inspect/validate/import/export/list with JSON, human output, stable exit codes, cancellation and stderr progress.
- [x] Demonstration and documentation: `samples/FailedTests` opt-in failing xUnit run, `.github/workflows/ci.yml`, README, LICENSE, CONTRIBUTING, SECURITY, package READMEs, architecture ADRs, bundle guide, implementation status. Build/test/pack and execute real failed-test capture.

## Verification

For each behavior: write the test, observe missing behavior fail, implement, run affected tests. After the milestone run `dotnet build PulseGrid.sln -c Release --no-restore`, `dotnet test PulseGrid.sln -c Release --no-build`, and `dotnet pack PulseGrid.sln -c Release --no-build`. CI repeats these on Windows and Linux. Validate the example and a real failed xUnit TRX capture. Hosted CI status is only known after publication.
