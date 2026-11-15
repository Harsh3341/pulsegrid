# Evidence bundle format 1.0.0

A .pgbundle is a ZIP archive containing:

```text
manifest.json
manifest.sha256
artifacts/sha256/<64 lowercase hex SHA-256 digits>
```

The manifest follows [the JSON Schema](../../specifications/evidence-bundle/1.0.0.schema.json). See [the example](../../specifications/examples/evidence-manifest.json). Every identifier is a nonempty UUID, timestamps are UTC, evidence references belong to the execution and evidence IDs are unique. The artifact digest hashes exact uncompressed bytes. The manifest checksum is exactly 64 ASCII lowercase hexadecimal characters, without a newline, hashing exact UTF-8 manifest bytes.

The current artifact payload follows [the test results schema](../../specifications/evidence-bundle/test-results-1.0.0.schema.json): a JSON array with required `id`, `method`, `outcome`, nullable `durationMilliseconds`, and nullable `diagnostics`. Outcomes are `passed`, `failed`, `skipped` or `inconclusive`. Diagnostic text is excluded; failed results have the redaction marker. Sensitivity is `redacted`. Unknown payload fields, missing outcomes and duplicate test IDs are rejected. Payload schemas and execution/result consistency are validated on every bundle read; artifact hashes establish byte consistency, not honesty of reported results.

Readers reject duplicate archive entries, unknown entries, traversal paths, absolute paths, backslashes, symbolic links, missing content, invalid lengths, duplicate JSON properties and unsupported versions. Unknown manifest fields are allowed within 1.0.0 and participate in the manifest digest. Import/export preserves exact original bytes.

Limits: 1 MiB manifest; 64 MiB per artifact, with the current test-results JSON payload limited to 8 MiB; 256 MiB compressed or expanded bundle; 1,026 entries; 1,024 evidence references; 10,000 unique test results. TRX ingestion is limited to 64 MiB XML and 10,000 tests/definitions. Hashing and artifact extraction stream data; manifest and typed test payload validation use bounded memory.

Creation/copy/extraction publish by renaming a flushed temporary file in the output directory. Existing outputs are refused. Cancellation or failure removes temporary output. A process crash may leave a hidden `*.tmp` file; it is never recognized as a bundle. Unix files are owner-readable/writable; Windows permissions inherit the selected directory's ACL.

CLI exit codes: 0 operation succeeded; 1 invalid evidence or I/O failure; 2 usage error; 130 cancellation. A captured test failure is an execution outcome, not a capture failure. `--json` writes one JSON document to stdout; progress/errors go to stderr. JSON command failures also return a failure document.

Use `export <bundle> --evidence <uuid> --output tests.json` for selective extraction. Extraction writes only the explicitly supplied destination; archive paths never choose filesystem destinations. `import` and whole-bundle `export` validate before publishing a copy. `list` validates each top-level bundle and fails on any invalid entry rather than silently hiding it.

Retention is local and manual in this milestone: select a controlled output directory and delete bundles when no longer needed. There are no automatic uploads, background retention jobs, signatures or remote stores. Set an appropriate filesystem ACL and disk quota. A replaced bundle with recomputed hashes can validate; trusted signatures and signer identity are future work.
