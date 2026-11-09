using System.IO.Enumeration;
using System.Text;
using System.Text.Json;
using PulseGrid.Core;
using PulseGrid.Evidence;
using PulseGrid.Evidence.Storage.Local;
using PulseGrid.Evidence.Xunit;
using PulseGrid.Specifications;

namespace PulseGrid.Cli;

/// <summary>The CLI composition root and command dispatch, with injectable output streams.</summary>
public sealed class CliApplication(TextWriter output, TextWriter error)
{
    private readonly LocalBundleStore store = new();

    /// <summary>Executes a command. Exit codes: success 0, invalid data/I/O 1, usage 2, cancellation 130.</summary>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var json = args.Contains("--json", StringComparer.Ordinal);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parsed = new Arguments(args);
            switch (parsed.Command)
            {
                case "help":
                    parsed.NoPositionals();
                    await Write(new { commands = new[] { "capture", "inspect", "validate", "export", "import", "list", "version" } },
                        "PulseGrid Evidence MVP\ncapture --source <file.trx|directory> --output <bundle.pgbundle> [--exclude <pattern>] [--revision <revision>] [--branch <branch>]\ninspect <bundle>\nvalidate <bundle>\nexport <bundle> --output <path> [--evidence <id>]\nimport <bundle> --output <new-bundle>\nlist [directory]\nversion\nAdd --json for machine-readable output.", json);
                    break;
                case "version":
                    parsed.NoPositionals();
                    await Write(new { version = "0.1.0", bundleSchema = "1.0.0" }, "PulseGrid 0.1.0 (bundle schema 1.0.0)", json);
                    break;
                case "capture":
                    parsed.NoPositionals();
                    var source = SelectSource(parsed.Required("source"), parsed.Options.GetValueOrDefault("exclude"));
                    await error.WriteLineAsync("Capturing selected TRX results; diagnostics and display parameters are excluded.");
                    var captured = await new CaptureService(new TrxEvidenceAdapter(), store).CaptureAsync(
                        source, parsed.Required("output"), parsed.Options.GetValueOrDefault("revision"), parsed.Options.GetValueOrDefault("branch"), cancellationToken);
                    await Write(captured, $"Created bundle {captured.BundleId}; execution {captured.Execution.Status}, complete={captured.Execution.IsComplete}.", json);
                    break;
                case "validate":
                    await error.WriteLineAsync("Validating schema and all SHA-256 hashes.");
                    var validated = await store.ReadAsync(parsed.OnePath(), cancellationToken);
                    await Write(new { valid = true, bundleId = validated.BundleId, executionStatus = validated.Execution.Status, trust = "unsigned; integrity is not execution trust" },
                        $"Valid bundle {validated.BundleId}. Integrity verified; execution trust has not been established.", json);
                    break;
                case "inspect":
                    var path = parsed.OnePath();
                    await error.WriteLineAsync("Validating bundle before inspection.");
                    var manifest = await store.ReadAsync(path, cancellationToken);
                    var tests = new List<TestResult>();
                    foreach (var evidence in manifest.Evidence)
                    {
                        using var content = new MemoryStream();
                        await store.ReadEvidenceAsync(path, evidence.Id, content, cancellationToken);
                        var results = TestResultsCodec.Deserialize(content.ToArray());
                        if (tests.Count + results.Count > 10_000 || results.Any(t => t is null || t.Id == Guid.Empty || string.IsNullOrWhiteSpace(t.Method) || !Enum.IsDefined(t.Outcome)))
                            throw new InvalidDataException("Malformed test results.");
                        tests.AddRange(results);
                    }
                    var human = new StringBuilder($"Bundle {manifest.BundleId}\nExecution {manifest.Execution.Id}: {manifest.Execution.Status}; complete={manifest.Execution.IsComplete}\n");
                    foreach (var test in tests) human.AppendLine($"{test.Outcome}: {ConsoleSafe(test.Method)} ({test.DurationMilliseconds?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"} ms)");
                    human.Append("Diagnostics were excluded during capture. Integrity does not establish execution trust.");
                    await Write(new { manifest, tests }, human.ToString(), json);
                    break;
                case "export":
                case "import":
                    var input = parsed.OnePath();
                    var destination = parsed.Required("output");
                    await error.WriteLineAsync("Validating evidence before writing the destination.");
                    if (parsed.Options.TryGetValue("evidence", out var id))
                    {
                        if (!Guid.TryParse(id, out var evidenceId)) throw new UsageException("Evidence ID must be a UUID.");
                        await store.ExtractAsync(input, evidenceId, destination, cancellationToken);
                    }
                    else await store.CopyAsync(input, destination, cancellationToken);
                    await Write(new { success = true, output = Path.GetFullPath(destination) }, "Wrote validated evidence to " + ConsoleSafe(destination), json);
                    break;
                case "list":
                    var directory = parsed.OnePath(".");
                    var bundles = new List<object>();
                    foreach (var file in Directory.EnumerateFiles(directory, "*.pgbundle", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await error.WriteLineAsync("Validating " + ConsoleSafe(Path.GetFileName(file)));
                        var item = await store.ReadAsync(file, cancellationToken);
                        bundles.Add(new { path = Path.GetFullPath(file), bundleId = item.BundleId, execution = item.Execution });
                    }
                    await Write(bundles, $"Found {bundles.Count} validated bundles.\n" + string.Join("\n", bundles.Select(b => JsonSerializer.Serialize(b, ManifestCodec.JsonOptions))), json);
                    break;
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            await ReportError("Operation cancelled; no new output was published unless an earlier command completed.", json);
            return 130;
        }
        catch (UsageException ex)
        {
            await ReportError(ex.Message, json);
            return 2;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or JsonException)
        {
            await ReportError(ex.Message, json);
            return 1;
        }
    }

    private async Task Write(object value, string human, bool json) =>
        await output.WriteLineAsync(json ? JsonSerializer.Serialize(value, ManifestCodec.JsonOptions) : human);

    private async Task ReportError(string message, bool json)
    {
        await error.WriteLineAsync("Error: " + ConsoleSafe(message));
        if (json) await output.WriteLineAsync(JsonSerializer.Serialize(new { success = false, error = message }, ManifestCodec.JsonOptions));
    }

    private static string SelectSource(string source, string? exclusion)
    {
        var files = Directory.Exists(source)
            ? Directory.EnumerateFiles(source, "*.trx", SearchOption.TopDirectoryOnly).ToArray()
            : [source];
        files = files.Where(file => exclusion is null || !FileSystemName.MatchesSimpleExpression(exclusion, Path.GetFileName(file), ignoreCase: true)).ToArray();
        if (files.Length != 1) throw new UsageException("Capture requires exactly one TRX report. Select a file or exclude unwanted reports.");
        if (!string.Equals(Path.GetExtension(files[0]), ".trx", StringComparison.OrdinalIgnoreCase)) throw new UsageException("Capture source must be a .trx report.");
        return files[0];
    }

    private static string ConsoleSafe(string value) =>
        new(value.Take(1024).Select(c => char.IsControl(c) ? '?' : c).ToArray());
}
