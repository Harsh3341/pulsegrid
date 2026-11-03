using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema;

namespace PulseGrid.Specifications;

/// <summary>Serializes and validates manifest schema and domain invariants.</summary>
public static class ManifestCodec
{
    private static readonly JsonSchema Schema = LoadSchema();

    /// <summary>Shared JSON wire settings. Unknown manifest fields are permitted in version 1.0.0.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = CreateOptions();

    /// <summary>Serializes only schema-valid, internally consistent metadata.</summary>
    public static byte[] Serialize(BundleManifest manifest)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        _ = Deserialize(bytes);
        return bytes;
    }

    /// <summary>Validates JSON Schema, version, UTC timestamps, references and identifiers.</summary>
    public static BundleManifest Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            if (bytes.Length > 1_048_576) throw new InvalidDataException("Manifest exceeds 1 MiB.");
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            RejectDuplicateProperties(document.RootElement);
            var node = JsonNode.Parse(bytes);
            if (!Schema.Evaluate(node).IsValid) throw new InvalidDataException("Manifest does not satisfy evidence schema 1.0.0.");
            var manifest = JsonSerializer.Deserialize<BundleManifest>(bytes, JsonOptions)
                ?? throw new InvalidDataException("Manifest is null.");
            if (manifest.BundleId == Guid.Empty || manifest.CreatedAt.Offset != TimeSpan.Zero || manifest.Execution is null)
                throw new InvalidDataException("Invalid bundle identifier or timestamp.");
            if (manifest.Evidence.Select(e => e.Id).Distinct().Count() != manifest.Evidence.Count)
                throw new InvalidDataException("Duplicate evidence identifiers.");
            foreach (var evidence in manifest.Evidence)
            {
                if (evidence.Id == Guid.Empty || evidence.ExecutionId != manifest.Execution.Id || evidence.Timestamp.Offset != TimeSpan.Zero)
                    throw new InvalidDataException("Invalid evidence identifier, execution reference or timestamp.");
            }
            return manifest;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or NotSupportedException)
        {
            throw new InvalidDataException("Invalid evidence manifest.", ex);
        }
    }

    internal static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property.");
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static JsonSchema LoadSchema()
    {
        using var stream = typeof(ManifestCodec).Assembly.GetManifestResourceStream("PulseGrid.EvidenceBundle.schema.json")
            ?? throw new InvalidOperationException("Embedded bundle schema is unavailable.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true, MaxDepth = 32, PropertyNameCaseInsensitive = false };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
