using System.IO.Compression;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using PulseGrid.Abstractions;
using PulseGrid.Core;
using PulseGrid.Evidence;
using PulseGrid.Evidence.Storage.Local;
using PulseGrid.Evidence.Xunit;
using Xunit;

namespace PulseGrid.Tests.Integration;

public sealed class BundleTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "pulsegrid-tests-" + Guid.NewGuid().ToString("N"));
    private readonly LocalBundleStore store = new();
    public BundleTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, recursive: true);
    private string PathFor(string name) => Path.Combine(root, name);

    private async Task<string> Capture()
    {
        var input = PathFor("results.trx");
        await File.WriteAllTextAsync(input, TrxFixtures.Run());
        var bundle = PathFor("failure.pgbundle");
        await new CaptureService(new TrxEvidenceAdapter(), store).CaptureAsync(input, bundle);
        return bundle;
    }

    [Fact]
    public async Task RoundTripRetainsFailureAndSanitizedTests()
    {
        var path = await Capture();
        var manifest = await store.ReadAsync(path);
        Assert.Equal(ExecutionStatus.Failed, manifest.Execution.Status);
        var evidence = Assert.Single(manifest.Evidence);
        var extracted = PathFor("tests.json");
        await store.ExtractAsync(path, evidence.Id, extracted);
        var content = await File.ReadAllTextAsync(extracted);
        Assert.Contains("Demo.PaymentTests.Retries", content);
        Assert.DoesNotContain("secret", content);
        Assert.DoesNotContain("do-not-store", content);
        using var json = JsonDocument.Parse(content);
        Assert.Equal("failed", json.RootElement[0].GetProperty("outcome").GetString());
        var copied = PathFor("portable.pgbundle");
        await store.CopyAsync(path, copied);
        Assert.Equal(await File.ReadAllBytesAsync(path), await File.ReadAllBytesAsync(copied));
        File.Delete(path);
        File.Delete(PathFor("results.trx"));
        Assert.Equal(manifest.BundleId, (await store.ReadAsync(copied)).BundleId);
    }

    [Theory]
    [InlineData("manifest.json")]
    [InlineData("artifact")]
    public async Task DetectsModifiedBytes(string target)
    {
        var path = await Capture();
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            var entry = target == "artifact" ? zip.Entries.Single(e => e.FullName.StartsWith("artifacts/")) : zip.GetEntry(target)!;
            var name = entry.FullName;
            entry.Delete();
            using var writer = new StreamWriter(zip.CreateEntry(name).Open());
            await writer.WriteAsync("modified");
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(path));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("/absolute")]
    [InlineData("C:/drive")]
    [InlineData("artifacts\\escape")]
    [InlineData("unlisted.txt")]
    [InlineData("manifest.json")]
    public async Task RejectsUnsafeUnlistedAndDuplicateEntries(string name)
    {
        var path = await Capture();
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            using var writer = new StreamWriter(zip.CreateEntry(name).Open());
            await writer.WriteAsync("unexpected");
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(path));
        var output = PathFor("should-not-exist");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CopyAsync(path, output));
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task MissingArtifactIsRejected()
    {
        var path = await Capture();
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            zip.Entries.Single(e => e.FullName.StartsWith("artifacts/")).Delete();
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(path));
    }

    [Fact]
    public async Task ExistingOutputIsNeverOverwritten()
    {
        var path = await Capture();
        var before = await File.ReadAllBytesAsync(path);
        await Assert.ThrowsAsync<IOException>(() => store.CopyAsync(path, path));
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task CancelledCaptureDoesNotPublishBundle()
    {
        var input = PathFor("results.trx");
        await File.WriteAllTextAsync(input, TrxFixtures.Run());
        var output = PathFor("cancelled.pgbundle");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CaptureService(new TrxEvidenceAdapter(), store).CaptureAsync(input, output, cancellationToken: new CancellationToken(true)));
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetFiles(root, "*.tmp"));
    }

    [Fact]
    public async Task ArtifactReadFailureDoesNotPublishOrLeakTemporaryFile()
    {
        var manifest = await store.ReadAsync(await Capture());
        var output = PathFor("interrupted.pgbundle");
        await Assert.ThrowsAsync<IOException>(() => store.CreateAsync(output, manifest,
            [new BundleArtifact(manifest.Evidence[0].Sha256, () => throw new IOException("interrupted"))]));
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetFiles(root, "*.tmp"));
    }

    [Fact]
    public async Task InvalidArtifactHashDoesNotPublishBundle()
    {
        var manifest = await store.ReadAsync(await Capture());
        var output = PathFor("wrong-hash.pgbundle");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CreateAsync(output, manifest,
            [new BundleArtifact(manifest.Evidence[0].Sha256, () => new MemoryStream("wrong"u8.ToArray()))]));
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task UnknownEvidenceCannotBeExtracted()
    {
        var path = await Capture();
        var output = PathFor("unknown.json");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ExtractAsync(path, Guid.NewGuid(), output));
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task OversizedManifestIsRejectedBeforeParsing()
    {
        var path = await Capture();
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            zip.GetEntry("manifest.json")!.Delete();
            using var entry = zip.CreateEntry("manifest.json").Open();
            await entry.WriteAsync(new byte[1_048_577]);
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(path));
    }

    [Fact]
    public async Task SymbolicLinkEntryIsRejected()
    {
        var path = await Capture();
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            zip.Entries.Single(e => e.FullName.StartsWith("artifacts/")).ExternalAttributes = unchecked((int)0xA1800000);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAsync(path));
    }

    [Fact]
    public async Task CancelledArtifactWriteRemovesTemporaryFile()
    {
        var manifest = await store.ReadAsync(await Capture());
        using var cancellation = new CancellationTokenSource();
        var output = PathFor("cancelled-write.pgbundle");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CreateAsync(output, manifest,
            [new BundleArtifact(manifest.Evidence[0].Sha256, () => { cancellation.Cancel(); return new MemoryStream("data"u8.ToArray()); })], cancellation.Token));
        Assert.False(File.Exists(output));
        Assert.Empty(Directory.GetFiles(root, "*.tmp"));
    }

    [Fact]
    public async Task HashValidPayloadWithMissingOutcomeIsRejectedAtStorageBoundary()
    {
        var manifest = await store.ReadAsync(await Capture());
        var bytes = Encoding.UTF8.GetBytes("[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"method\":\"Demo.Tests.Run\"}]");
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var malformed = manifest with { Evidence = [manifest.Evidence[0] with { Sha256 = hash, Length = bytes.Length }] };
        var output = PathFor("missing-outcome.pgbundle");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CreateAsync(output, malformed,
            [new BundleArtifact(hash, () => new MemoryStream(bytes))]));
        Assert.False(File.Exists(output));
    }

    [Fact]
    public async Task PassingManifestCannotOverrideFailedEvidence()
    {
        var path = await Capture();
        var manifest = await store.ReadAsync(path);
        using var content = new MemoryStream();
        await store.ReadEvidenceAsync(path, manifest.Evidence[0].Id, content);
        var bytes = content.ToArray();
        var contradictory = manifest with { Execution = new PulseGrid.Core.Execution(manifest.Execution.Id,
            manifest.Execution.StartedAt, manifest.Execution.CompletedAt, ExecutionStatus.Passed, true) };
        var output = PathFor("false-pass.pgbundle");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CreateAsync(output, contradictory,
            [new BundleArtifact(manifest.Evidence[0].Sha256, () => new MemoryStream(bytes))]));
        Assert.False(File.Exists(output));
    }

    [Theory]
    [InlineData("[unavailable]", "passed", ExecutionStatus.Passed)]
    [InlineData("Demo.Tests.Run", "inconclusive", ExecutionStatus.Inconclusive)]
    public async Task UnknownIdentifierOrOutcomeCannotBeDeclaredComplete(string method, string outcome, ExecutionStatus status)
    {
        var manifest = await store.ReadAsync(await Capture());
        var bytes = Encoding.UTF8.GetBytes($"[{{\"id\":\"10000000-0000-0000-0000-000000000001\",\"method\":\"{method}\",\"outcome\":\"{outcome}\",\"durationMilliseconds\":null,\"diagnostics\":null}}]");
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var malformed = manifest with
        {
            Execution = new Execution(manifest.Execution.Id, manifest.Execution.StartedAt, manifest.Execution.CompletedAt, status, true),
            Evidence = [manifest.Evidence[0] with { Sha256 = hash, Length = bytes.Length }]
        };
        var output = PathFor("incomplete-results.pgbundle");
        await Assert.ThrowsAsync<InvalidDataException>(() => store.CreateAsync(output, malformed,
            [new BundleArtifact(hash, () => new MemoryStream(bytes))]));
        Assert.False(File.Exists(output));
    }
}
