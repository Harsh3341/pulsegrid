using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using PulseGrid.Core;

namespace PulseGrid.Specifications;

/// <summary>Validates sanitized test payloads before interpreting any outcome.</summary>
public static class TestResultsCodec
{
    private static readonly JsonSchema Schema = LoadSchema();

    /// <summary>Serializes validated sanitized test results.</summary>
    public static byte[] Serialize(IReadOnlyList<TestResult> tests)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(tests, ManifestCodec.JsonOptions);
        _ = Deserialize(bytes);
        return bytes;
    }

    /// <summary>Rejects missing outcomes, unknown fields, unsafe diagnostics and duplicate result IDs.</summary>
    public static IReadOnlyList<TestResult> Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > 8 * 1024 * 1024) throw new InvalidDataException("Test results exceed 8 MiB.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            ManifestCodec.RejectDuplicateProperties(document.RootElement);
            if (!Schema.Evaluate(JsonNode.Parse(bytes)).IsValid) throw new InvalidDataException("Test results do not satisfy payload schema 1.0.0.");
            var results = JsonSerializer.Deserialize<TestResult[]>(bytes, ManifestCodec.JsonOptions)
                ?? throw new InvalidDataException("Test results are null.");
            if (results.Any(t => t.Id == Guid.Empty || string.IsNullOrWhiteSpace(t.Method)) || results.Select(t => t.Id).Distinct().Count() != results.Length)
                throw new InvalidDataException("Test result IDs must be nonempty and unique.");
            return results;
        }
        catch (JsonException ex) { throw new InvalidDataException("Malformed test results JSON.", ex); }
    }

    private static JsonSchema LoadSchema()
    {
        using var stream = typeof(TestResultsCodec).Assembly.GetManifestResourceStream("PulseGrid.TestResults.schema.json")
            ?? throw new InvalidOperationException("Embedded test results schema is unavailable.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }
}
