using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PulseGrid.Abstractions;
using PulseGrid.Specifications;

namespace PulseGrid.Evidence.Storage.Local;

/// <summary>ZIP evidence bundles with atomic publication and validated content-addressed artifacts.</summary>
public sealed class LocalBundleStore : IBundleStore
{
    /// <inheritdoc />
    public Task CreateAsync(string output, BundleManifest manifest, IReadOnlyCollection<BundleArtifact> artifacts, CancellationToken cancellationToken = default)
    {
        var manifestBytes = ManifestCodec.Serialize(manifest);
        var expected = manifest.Evidence.Select(e => e.Sha256).ToHashSet(StringComparer.Ordinal);
        if (artifacts.Select(a => a.Sha256).Distinct(StringComparer.Ordinal).Count() != artifacts.Count
            || !expected.SetEquals(artifacts.Select(a => a.Sha256)))
            throw new InvalidDataException("Artifacts must exactly match the manifest index.");
        return AtomicFile.WriteAsync(output, async stream =>
        {
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                await WriteEntry(zip, "manifest.json", manifestBytes, cancellationToken);
                await WriteEntry(zip, "manifest.sha256", Encoding.ASCII.GetBytes(Convert.ToHexStringLower(SHA256.HashData(manifestBytes))), cancellationToken);
                foreach (var artifact in artifacts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await using var input = artifact.OpenRead();
                    await using var entry = zip.CreateEntry("artifacts/sha256/" + artifact.Sha256, CompressionLevel.Optimal).Open();
                    var (hash, length) = await ArchiveValidator.CopyAndHashAsync(input, entry, ArchiveValidator.MaxArtifactBytes, cancellationToken);
                    if (hash != artifact.Sha256 || manifest.Evidence.Any(e => e.Sha256 == hash && e.Length != length))
                        throw new InvalidDataException("Artifact length or hash does not match the manifest.");
                }
            }
            stream.Position = 0;
            _ = await ArchiveValidator.ValidateAsync(stream, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BundleManifest> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = OpenRead(path);
        return await ArchiveValidator.ValidateAsync(stream, cancellationToken);
    }

    /// <inheritdoc />
    public Task CopyAsync(string source, string output, CancellationToken cancellationToken = default) =>
        AtomicFile.WriteAsync(output, async destination =>
        {
            await using var input = OpenRead(source);
            if (input.Length > 256 * 1024 * 1024) throw new InvalidDataException("Bundle exceeds 256 MiB.");
            await input.CopyToAsync(destination, cancellationToken);
            destination.Position = 0;
            _ = await ArchiveValidator.ValidateAsync(destination, cancellationToken);
        }, cancellationToken);

    /// <inheritdoc />
    public async Task ReadEvidenceAsync(string source, Guid evidenceId, Stream output, CancellationToken cancellationToken = default)
    {
        await using var input = OpenRead(source);
        var manifest = await ArchiveValidator.ValidateAsync(input, cancellationToken);
        var evidence = manifest.Evidence.SingleOrDefault(e => e.Id == evidenceId)
            ?? throw new InvalidDataException("Evidence ID was not found in this bundle.");
        input.Position = 0;
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        var entry = zip.GetEntry("artifacts/sha256/" + evidence.Sha256)
            ?? throw new InvalidDataException("Evidence content is missing.");
        await using var artifact = entry.Open();
        var (hash, length) = await ArchiveValidator.CopyAndHashAsync(artifact, output, ArchiveValidator.MaxArtifactBytes, cancellationToken);
        if (hash != evidence.Sha256 || length != evidence.Length) throw new InvalidDataException("Evidence changed during extraction.");
    }

    /// <inheritdoc />
    public Task ExtractAsync(string source, Guid evidenceId, string output, CancellationToken cancellationToken = default) =>
        AtomicFile.WriteAsync(output, destination => ReadEvidenceAsync(source, evidenceId, destination, cancellationToken), cancellationToken);

    private static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static async Task WriteEntry(ZipArchive zip, string name, byte[] bytes, CancellationToken cancellationToken)
    {
        await using var entry = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
        await entry.WriteAsync(bytes, cancellationToken);
    }
}
