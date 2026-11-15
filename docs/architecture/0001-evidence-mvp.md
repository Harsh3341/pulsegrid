# ADR 0001: Local-first Evidence MVP

Status: Accepted. Defines the architecture for Milestone 1 — Evidence MVP.

## Decision

Use a modular .NET 10 application with seven functional production projects. Dependencies point inward: Core is independent; Specifications uses Core; Abstractions uses both; Evidence uses the contracts; local storage and the xUnit adapter implement the contracts; CLI composes them. There is no dependency on a DI container: construction is explicit because this composition root is small. No future module is scaffolded.

Use ZIP for .pgbundle transport, UTF-8 JSON for manifest/results, JSON Schema draft 2020-12 and SHA-256 content addressing. Standards-based schema evaluation uses JsonSchema.Net; ZIP, XML, hashing and CLI parsing use the .NET base libraries. A CLI framework, database and telemetry SDK are unnecessary for this milestone.

Writes use a private temporary file in the destination directory, flush to disk and rename without replacement. Every read validates archive layout, schema, manifest digest and all artifact hashes. Imported/exported whole bundles retain exact original bytes, including unknown manifest fields.

No signature, environment discovery, reproduction, verification, remote storage, server or dashboard is implemented. These remain distinct roadmap milestones. Integrity establishes consistency with recorded hashes, not provenance or execution trust. Source Link is supported by SDK source-control integration once a hosted Git repository is available; reproducible builds and source metadata must be verified during release packaging.

## Consequences

The local workflow remains independently usable and future adapters can reuse its contracts. The current schema accepts exactly 1.0.0; unknown fields within that version are allowed, duplicates are rejected. Optional future fields may be ignored by typed consumers, so whole-bundle copies must preserve bytes. Schema compatibility changes require tests and an ADR.
