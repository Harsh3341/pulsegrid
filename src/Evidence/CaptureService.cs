using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using PulseGrid.Abstractions;
using PulseGrid.Core;
using PulseGrid.Specifications;

namespace PulseGrid.Evidence;

/// <summary>Coordinates an adapter and storage without depending on their implementations.</summary>
public sealed class CaptureService(IEvidenceAdapter adapter, IBundleStore store)
{
    /// <summary>Captures one explicitly selected TRX file into a new portable bundle.</summary>
    public async Task<BundleManifest> CaptureAsync(string source, string output, string? sourceRevision = null, string? branch = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateMetadata(sourceRevision);
        ValidateMetadata(branch);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var run = await adapter.CaptureAsync(input, cancellationToken);
        var execution = new Execution(run.Execution.Id, run.Execution.StartedAt, run.Execution.CompletedAt,
            run.Execution.Status, run.Execution.IsComplete, sourceRevision, branch);
        var bytes = TestResultsCodec.Serialize(run.Tests);
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var now = DateTimeOffset.UtcNow;
        var evidence = new EvidenceReference(Guid.NewGuid(), execution.Id, "test-results", "application/json", now, adapter.Name, hash, bytes.LongLength, "redacted");
        var manifest = new BundleManifest("1.0.0", Guid.NewGuid(), now, execution, [evidence]);
        await store.CreateAsync(output, manifest, [new BundleArtifact(hash, () => new MemoryStream(bytes, writable: false))], cancellationToken);
        return manifest;
    }

    private static void ValidateMetadata(string? value)
    {
        if (value is not null && (value.Length > 256 || !Regex.IsMatch(value, @"^[A-Za-z0-9._/-]+$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))))
            throw new ArgumentException("Revision and branch metadata must be 1–256 letters, digits, dots, underscores, slashes or hyphens.");
    }
}
