using System.Buffers;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PulseGrid.Specifications;
using PulseGrid.Core;

namespace PulseGrid.Evidence.Storage.Local;

internal static class ArchiveValidator
{
    internal const long MaxArtifactBytes = 64 * 1024 * 1024;
    private const long MaxBundleBytes = 256 * 1024 * 1024;

    internal static async Task<BundleManifest> ValidateAsync(Stream stream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (stream.Length > MaxBundleBytes) throw new InvalidDataException("Bundle exceeds 256 MiB.");
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > 1026) throw new InvalidDataException("Too many bundle entries.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = entry.FullName;
            if (name != "manifest.json" && name != "manifest.sha256"
                && !Regex.IsMatch(name, @"^artifacts/sha256/[a-f0-9]{64}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
                throw new InvalidDataException("Unsafe or unsupported archive entry path.");
            if (!entries.TryAdd(name, entry)) throw new InvalidDataException("Duplicate archive entry.");
            // Reject Unix symbolic links even though extraction never uses entry paths.
            if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000) throw new InvalidDataException("Symbolic link archive entries are forbidden.");
            var limit = name == "manifest.json" ? 1_048_576L : name == "manifest.sha256" ? 64L : MaxArtifactBytes;
            if (entry.Length > limit || entry.Length < 0) throw new InvalidDataException("Archive entry exceeds its size limit.");
            total = checked(total + entry.Length);
            if (total > MaxBundleBytes) throw new InvalidDataException("Expanded bundle exceeds 256 MiB.");
        }
        if (!entries.TryGetValue("manifest.json", out var manifestEntry) || !entries.TryGetValue("manifest.sha256", out var checksumEntry))
            throw new InvalidDataException("Manifest or manifest checksum is missing.");
        var manifestBytes = await ReadSmall(manifestEntry, 1_048_576, cancellationToken);
        var checksum = Encoding.ASCII.GetString(await ReadSmall(checksumEntry, 64, cancellationToken));
        if (checksum != Convert.ToHexStringLower(SHA256.HashData(manifestBytes)))
            throw new InvalidDataException("Manifest SHA-256 does not match.");
        var manifest = ManifestCodec.Deserialize(manifestBytes);
        var expected = manifest.Evidence.Select(e => "artifacts/sha256/" + e.Sha256).ToHashSet(StringComparer.Ordinal);
        expected.Add("manifest.json");
        expected.Add("manifest.sha256");
        if (!expected.SetEquals(entries.Keys)) throw new InvalidDataException("Bundle contents do not match the evidence index.");
        var testIds = new HashSet<Guid>();
        var anyFailed = false;
        var allPassed = true;
        var incompleteResults = false;
        foreach (var references in manifest.Evidence.GroupBy(e => e.Sha256))
        {
            var entry = entries["artifacts/sha256/" + references.Key];
            await using var content = entry.Open();
            var (hash, length) = await CopyAndHashAsync(content, Stream.Null, MaxArtifactBytes, cancellationToken);
            if (hash != references.Key || length != entry.Length || references.Any(e => e.Length != length))
                throw new InvalidDataException("Artifact SHA-256 or length does not match.");
            var tests = TestResultsCodec.Deserialize(await ReadSmall(entry, 8 * 1024 * 1024, cancellationToken));
            foreach (var test in tests)
            {
                if (!testIds.Add(test.Id) || testIds.Count > 10_000) throw new InvalidDataException("Duplicate or excessive test result IDs.");
                anyFailed |= test.Outcome == TestOutcome.Failed;
                allPassed &= test.Outcome == TestOutcome.Passed;
                incompleteResults |= test.Method == "[unavailable]" || test.Outcome == TestOutcome.Inconclusive;
            }
        }
        if (anyFailed != (manifest.Execution.Status == ExecutionStatus.Failed)
            || manifest.Execution.IsComplete && (testIds.Count == 0 || incompleteResults)
            || manifest.Execution.Status == ExecutionStatus.Passed && (testIds.Count == 0 || !allPassed))
            throw new InvalidDataException("Execution status conflicts with the test evidence.");
        return manifest;
    }

    private static async Task<byte[]> ReadSmall(ZipArchiveEntry entry, int limit, CancellationToken cancellationToken)
    {
        await using var input = entry.Open();
        using var output = new MemoryStream();
        var (_, length) = await CopyAndHashAsync(input, output, limit, cancellationToken);
        if (length != entry.Length) throw new InvalidDataException("Archive entry length does not match.");
        return output.ToArray();
    }

    internal static async Task<(string Hash, long Length)> CopyAndHashAsync(Stream input, Stream output, long limit, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(65_536);
        long length = 0;
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, 65_536), cancellationToken)) != 0)
            {
                length += read;
                if (length > limit) throw new InvalidDataException("Artifact exceeds its size limit.");
                hash.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
            return (Convert.ToHexStringLower(hash.GetHashAndReset()), length);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer, clearArray: true); }
    }
}
