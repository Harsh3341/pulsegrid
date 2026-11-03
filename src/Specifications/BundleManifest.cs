using PulseGrid.Core;

namespace PulseGrid.Specifications;

/// <summary>Versioned, language-neutral portable bundle metadata.</summary>
/// <param name="SchemaVersion">Exact manifest schema version.</param>
/// <param name="BundleId">Stable bundle identifier.</param>
/// <param name="CreatedAt">UTC capture timestamp.</param>
/// <param name="Execution">Observed execution.</param>
/// <param name="Evidence">Content-addressed evidence index.</param>
public sealed record BundleManifest(string SchemaVersion, Guid BundleId, DateTimeOffset CreatedAt, Execution Execution, IReadOnlyList<EvidenceReference> Evidence);

/// <summary>Reference to a sanitized artifact inside a bundle.</summary>
/// <param name="Id">Evidence identifier.</param>
/// <param name="ExecutionId">Owning execution identifier.</param>
/// <param name="Type">Evidence category.</param>
/// <param name="ContentType">Artifact media type.</param>
/// <param name="Timestamp">UTC capture timestamp.</param>
/// <param name="Source">Adapter identity, never an absolute source path.</param>
/// <param name="Sha256">Lowercase SHA-256 digest of stored bytes.</param>
/// <param name="Length">Uncompressed byte length.</param>
/// <param name="Sensitivity">Redaction classification.</param>
public sealed record EvidenceReference(Guid Id, Guid ExecutionId, string Type, string ContentType, DateTimeOffset Timestamp, string Source, string Sha256, long Length, string Sensitivity);
