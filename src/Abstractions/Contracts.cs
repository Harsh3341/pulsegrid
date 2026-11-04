using PulseGrid.Core;
using PulseGrid.Specifications;

namespace PulseGrid.Abstractions;

/// <summary>Converts explicitly supplied execution evidence into sanitized results.</summary>
public interface IEvidenceAdapter
{
    /// <summary>Stable adapter identity for provenance.</summary>
    string Name { get; }
    /// <summary>Captures from a caller-owned readable stream without executing commands.</summary>
    Task<CapturedRun> CaptureAsync(Stream input, CancellationToken cancellationToken = default);
}

/// <summary>A repeatably readable artifact. Each call must supply a fresh stream.</summary>
/// <param name="Sha256">Expected lowercase SHA-256 digest.</param>
/// <param name="OpenRead">Opens a stream that the store will dispose.</param>
public sealed record BundleArtifact(string Sha256, Func<Stream> OpenRead);

/// <summary>Portable evidence storage, with validation at every read boundary.</summary>
public interface IBundleStore
{
    /// <summary>Atomically creates a new bundle, refusing to overwrite an existing file.</summary>
    Task CreateAsync(string output, BundleManifest manifest, IReadOnlyCollection<BundleArtifact> artifacts, CancellationToken cancellationToken = default);
    /// <summary>Validates schema, archive layout and every content hash before returning metadata.</summary>
    Task<BundleManifest> ReadAsync(string path, CancellationToken cancellationToken = default);
    /// <summary>Streams selected validated evidence to caller-owned output. Discard partial output on failure.</summary>
    Task ReadEvidenceAsync(string source, Guid evidenceId, Stream output, CancellationToken cancellationToken = default);
    /// <summary>Copies a fully validated bundle to a new destination.</summary>
    Task CopyAsync(string source, string output, CancellationToken cancellationToken = default);
    /// <summary>Extracts selected validated evidence to an explicitly named new file.</summary>
    Task ExtractAsync(string source, Guid evidenceId, string output, CancellationToken cancellationToken = default);
}
