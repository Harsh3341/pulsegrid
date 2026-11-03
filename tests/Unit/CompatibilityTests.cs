using System.Text;
using System.Text.Json.Nodes;
using PulseGrid.Specifications;
using Xunit;

namespace PulseGrid.Tests.Unit;

public class CompatibilityTests
{
    private static string Example => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "evidence-manifest.json"));

    [Fact]
    public void ExampleValidatesAgainstPublishedAndEmbeddedSchema()
    {
        var manifest = ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(Example));
        Assert.False(manifest.Execution.IsComplete);
        Assert.Equal(2, Assert.Single(manifest.Evidence).Length);
    }

    [Fact]
    public void UnknownFieldsAreAllowedWithinCurrentVersion()
    {
        var node = JsonNode.Parse(Example)!;
        node["futureMetadata"] = new JsonObject { ["tool"] = "other" };
        var manifest = ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString()));
        Assert.Equal(Guid.Parse("10000000-0000-0000-0000-000000000001"), manifest.BundleId);
    }

    [Fact]
    public void UnknownCaseVariantCannotShadowSchemaValidatedField()
    {
        var node = JsonNode.Parse(Example)!;
        node["execution"]!["SourceRevision"] = "unknown-field";
        Assert.Null(ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())).Execution.SourceRevision);
    }

    [Fact]
    public void DuplicateJsonPropertiesAreRejected() =>
        Assert.Throws<InvalidDataException>(() => ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(Example.Replace("\"schemaVersion\": \"1.0.0\"", "\"schemaVersion\": \"1.0.0\", \"schemaVersion\": \"1.0.0\""))));

    [Fact]
    public void EvidenceCannotReferToAnotherExecution()
    {
        var node = JsonNode.Parse(Example)!;
        node["evidence"]![0]!["executionId"] = "40000000-0000-0000-0000-000000000001";
        Assert.Throws<InvalidDataException>(() => ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())));
    }

    [Fact]
    public void NonUtcManifestTimestampIsRejected() =>
        Assert.Throws<InvalidDataException>(() => ManifestCodec.Deserialize(Encoding.UTF8.GetBytes(Example.Replace("08:00:00Z", "08:00:00+02:00"))));
}
