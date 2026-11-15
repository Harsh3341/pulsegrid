# ADR 0002: xUnit capture through TRX

Status: Accepted. Implements the Milestone 1 xUnit adapter and default sensitive-data exclusion.

## Decision

Ingest TRX produced by an xUnit test project using `dotnet test --logger trx` (VSTest). TRX is an existing language-neutral result format. This avoids dependencies on xUnit v2/v3 internals and keeps capture separate from execution authorization. The adapter contract is streaming and test-framework neutral; native xUnit XML and Microsoft.Testing.Platform extensions can be added independently.

Store only stable class/method identifiers, runner identifiers, timestamps, outcomes and optional duration. Do not store raw report bytes, display parameter values, messages, exception stacks, stdout or stderr. Failed diagnostics become `[REDACTED]`. Unknown or missing identifiers/outcomes/counters, unrecognized summaries and run-level errors make evidence incomplete; skipped tests produce an inconclusive execution. Observed failures retain failed status even if evidence is incomplete. VSTest can emit a run-level error for an ordinary failing test; without retaining/interpreting sensitive diagnostic text, that execution is conservatively incomplete.

One TRX execution maps to one bundle. Directories must contain exactly one selected top-level report; a filename exclusion pattern is supported. Combining independent executions is deferred until parent/child execution semantics are defined.

## Limitations

TRX cannot independently prove the originating framework or honesty of the runner. Unicode/generated identifiers outside the conservative allowlist are unavailable and make capture inconclusive. A producer can intentionally encode sensitive data in a stable identifier; automatic exclusion cannot infer that. Review retained identifiers before sharing. Original diagnostics remain only in the producer's report, under its retention controls. Capture never executes a command or uploads evidence.
