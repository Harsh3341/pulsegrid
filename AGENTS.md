# PulseGrid repository guidance

Read docs/implementation-status.md and docs/architecture/0001-evidence-mvp.md before changing behavior. They document the public implementation scope and accepted architecture. Maintainer checkouts may also contain a private product plan; it is locally excluded and must not be committed or required by public contributor workflows.

- Primary stack: .NET 10 LTS, C# 14. Build with the SDK selected by global.json.
- Work incrementally against the milestones in docs/implementation-status.md. Current scope: Milestone 1, Evidence MVP.
- Dependencies point inward: Core has no project dependencies; Specifications depends on Core; Abstractions depends on Core and Specifications; Evidence depends on these contracts; adapters and storage implement contracts; CLI composes modules.
- Keep environments, verification, remote storage, server, dashboard and MCP out of this milestone.
- Create projects only for working functionality. Do not add speculative interfaces or empty modules.
- Document public APIs with XML comments, use nullable references, central package versions and deterministic builds.
- Capture only explicitly selected result files. Never persist raw TRX, test output, exception text, environment variables, credentials or source files by default.
- Validate bundle schema, hashes and archive paths before inspection, import or extraction. Integrity is not execution trust.
- Missing, skipped or incomplete results must not silently produce a passing execution.
- Write meaningful unit/integration tests. Use real filesystem operations and representative TRX fixtures.
- Restore: dotnet restore PulseGrid.sln --configfile NuGet.Config
- Build: dotnet build PulseGrid.sln -c Release --no-restore
- Test: dotnet test PulseGrid.sln -c Release --no-build
- Pack: dotnet pack PulseGrid.sln -c Release --no-build
- Keep package READMEs, bundle format guide, ADRs and implementation status current.
- Do not upload evidence, execute captured commands or publish releases without explicit authorization.

