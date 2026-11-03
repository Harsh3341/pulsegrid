# PulseGrid.Specifications

Evidence manifest serialization, JSON Schema validation and domain consistency checks for .NET 10. Schema: `specifications/evidence-bundle/1.0.0.schema.json`.

After a release: `dotnet add package PulseGrid.Specifications --version 0.1.0`.
Use `ManifestCodec.Serialize` and `ManifestCodec.Deserialize` for manifests, and `TestResultsCodec` for explicit outcomes and sanitized payload validation at trust boundaries.
JsonSchema.Net supplies standards-based JSON Schema evaluation; the format itself is language-neutral.
