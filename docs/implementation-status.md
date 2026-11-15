# Implementation status

Updated: 15 November 2026.

## Milestone 0 foundation

Repository and development branch initialized; architecture decisions, versioned schema, coding conventions, central packages, contribution/security guidance and Windows/Linux CI are present. Seven production projects contain working behavior. The public repository uses `development` as its default branch with required Windows/Linux checks, one approving review, resolved review threads, linear history and blocked force pushes/deletion.

## Milestone 1 — Evidence MVP

Implemented: validated Core execution models; versioned JSON manifest and schema validation; extension contracts; portable ZIP bundle creation and inspection; SHA-256 manifest/artifact validation; atomic local storage; validated import/export and selective extraction; xUnit TRX ingestion; CLI capture/inspect/validate/import/export/list; human and JSON output, cancellation, explicit source selection and default diagnostic exclusion.

Evidence: Release solution builds without warnings/errors; all 57 unit/integration tests pass. A real deliberate failed xUnit test run produces TRX, is captured as a failed execution, and its bundle can be inspected and validated. Packages build and the packed CLI installs locally. See eng/verify-sample.ps1 for the repeatable real-run demonstration. Run-level errors conservatively make evidence incomplete, even when the emitted failed test is captured.

The integration suite covers filesystem round trips, portable copies without original reports, manifest/artifact tampering, missing/duplicate/unsafe archive entries, symlinks, oversized manifests, interrupted and cancelled writes, overwrite prevention, CLI workflows and error codes. Unit tests cover domain invariants, TRX outcomes, default redaction, unsafe XML, schema examples, unknown fields and duplicate/shadowed properties.

## Scope and limitations

One report per bundle; combining independent runs is deferred. The adapter targets xUnit using VSTest TRX, not native xUnit XML or Microsoft.Testing.Platform. Capture never runs a command. Logs, traces and arbitrary artifacts are not captured; diagnostic text and display parameters are excluded. Stable identifiers and explicit metadata still require review before sharing.

Unsigned hashes demonstrate internal consistency, not producer trust. Retention is manual. Windows permissions inherit destination ACLs; Unix outputs are owner-only. Atomic rename protects readers from interrupted publication; sudden process termination may leave a hidden temporary file. [Hosted Windows/Linux CI has passed](https://github.com/Harsh3341/pulsegrid/actions/runs/43714771262). Cross-machine portability is tested by moving a bundle and removing the originating path; CI exercises both operating systems.

## Next milestone

Milestone 2 — Environment MVP: environment manifest/schema, explicit runtime/SDK discovery, dependency inventory, selected-property fingerprints, classified comparison and Docker integration. Attach environment metadata to evidence through versioned contracts. Add compatibility and secret-exclusion tests before expanding capture.

Verify, reproduction, MCP, remote storage, server, dashboard and additional CI/test adapters remain later milestones.
