using System.Text.Json;
using PulseGrid.Cli;
using Xunit;

namespace PulseGrid.Tests.Integration;

public sealed class CliTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "pulsegrid-cli-" + Guid.NewGuid().ToString("N"));
    public CliTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, recursive: true);
    private string PathFor(string name) => Path.Combine(root, name);

    private static async Task<(int Code, string Output, string Error)> Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = await new CliApplication(output, error).RunAsync(args);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public async Task CapturesInspectsValidatesExportsImportsAndListsFailedExecution()
    {
        var input = PathFor("results.trx");
        var bundle = PathFor("failure.pgbundle");
        await File.WriteAllTextAsync(input, TrxFixtures.Run());
        var captured = await Run("capture", "--source", input, "--output", bundle, "--json");
        Assert.Equal(0, captured.Code);
        using var captureJson = JsonDocument.Parse(captured.Output);
        Assert.Equal("failed", captureJson.RootElement.GetProperty("execution").GetProperty("status").GetString());
        var inspected = await Run("inspect", bundle, "--json");
        Assert.Equal(0, inspected.Code);
        Assert.Contains("Demo.PaymentTests.Retries", inspected.Output);
        Assert.DoesNotContain("secret", inspected.Output);
        using var inspectJson = JsonDocument.Parse(inspected.Output);
        var validated = await Run("validate", bundle, "--json");
        Assert.Equal(0, validated.Code);
        using var validationJson = JsonDocument.Parse(validated.Output);
        Assert.True(validationJson.RootElement.GetProperty("valid").GetBoolean());
        var exported = PathFor("exported.pgbundle");
        Assert.Equal(0, (await Run("export", bundle, "--output", exported)).Code);
        var imported = PathFor("imported.pgbundle");
        Assert.Equal(0, (await Run("import", exported, "--output", imported)).Code);
        var evidence = captureJson.RootElement.GetProperty("evidence")[0].GetProperty("id").GetString()!;
        var extracted = PathFor("tests.json");
        Assert.Equal(0, (await Run("export", bundle, "--evidence", evidence, "--output", extracted)).Code);
        Assert.Contains("Retries", await File.ReadAllTextAsync(extracted));
        var listed = await Run("list", root, "--json");
        Assert.Equal(0, listed.Code);
        using var listJson = JsonDocument.Parse(listed.Output);
        Assert.Equal(3, listJson.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData("capture", "--unknown")]
    [InlineData("capture")]
    [InlineData("inspect")]
    [InlineData("verify")]
    public async Task InvalidArgumentsHaveUsageExitCode(params string[] args) =>
        Assert.Equal(2, (await Run(args)).Code);

    [Fact]
    public async Task InvalidBundleReturnsStructuredFailure()
    {
        var path = PathFor("invalid.pgbundle");
        await File.WriteAllTextAsync(path, "invalid");
        var result = await Run("validate", path, "--json");
        Assert.Equal(1, result.Code);
        using var json = JsonDocument.Parse(result.Output);
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task MultipleReportsRequireExplicitSelection()
    {
        await File.WriteAllTextAsync(PathFor("one.trx"), TrxFixtures.Run());
        await File.WriteAllTextAsync(PathFor("two.trx"), TrxFixtures.Run());
        Assert.Equal(2, (await Run("capture", "--source", root, "--output", PathFor("output.pgbundle"))).Code);
        Assert.False(File.Exists(PathFor("output.pgbundle")));
    }

    [Fact]
    public async Task ExclusionFiltersExplicitlySelectedReports()
    {
        await File.WriteAllTextAsync(PathFor("one.trx"), TrxFixtures.Run());
        await File.WriteAllTextAsync(PathFor("private.trx"), TrxFixtures.Run());
        Assert.Equal(0, (await Run("capture", "--source", root, "--exclude", "private*", "--output", PathFor("output.pgbundle"))).Code);
    }

    [Fact]
    public async Task CancellationHasDedicatedExitCode()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(130, await new CliApplication(output, error).RunAsync(["list", root], new CancellationToken(true)));
    }
}
