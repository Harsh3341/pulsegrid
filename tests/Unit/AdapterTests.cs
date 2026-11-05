using System.Text;
using System.Text.Json;
using PulseGrid.Core;
using PulseGrid.Evidence.Xunit;
using Xunit;

namespace PulseGrid.Tests.Unit;

public class AdapterTests
{
    private static Task<CapturedRun> Capture(string xml, CancellationToken token = default) =>
        new TrxEvidenceAdapter().CaptureAsync(new MemoryStream(Encoding.UTF8.GetBytes(xml)), token);

    [Fact]
    public async Task CapturesFailedTestWithoutSensitiveDiagnostics()
    {
        var run = await Capture(TrxFixtures.Run());
        Assert.Equal(ExecutionStatus.Failed, run.Execution.Status);
        Assert.True(run.Execution.IsComplete);
        var result = Assert.Single(run.Tests);
        Assert.Equal("Demo.PaymentTests.Retries", result.Method);
        Assert.Equal(125, result.DurationMilliseconds);
        Assert.Equal("[REDACTED]", result.Diagnostics);
        var json = JsonSerializer.Serialize(run);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("do-not-store", json);
    }

    [Theory]
    [InlineData("Passed", 1, ExecutionStatus.Passed, true)]
    [InlineData("Passed", 2, ExecutionStatus.Inconclusive, false)]
    [InlineData("NotExecuted", 1, ExecutionStatus.Inconclusive, true)]
    [InlineData("Timeout", 1, ExecutionStatus.Inconclusive, false)]
    public async Task MissingOrUnexecutedResultsDoNotPass(string outcome, int total, ExecutionStatus expected, bool complete)
    {
        var run = await Capture(TrxFixtures.Run(outcome, total));
        Assert.Equal(expected, run.Execution.Status);
        Assert.Equal(complete, run.Execution.IsComplete);
    }

    [Fact]
    public async Task MissingMethodDefinitionMakesPassingResultInconclusive()
    {
        var run = await Capture(TrxFixtures.Run("Passed", method: "Contains(secret)"));
        Assert.Equal(ExecutionStatus.Inconclusive, run.Execution.Status);
        Assert.Equal("[unavailable]", run.Tests[0].Method);
    }

    [Fact]
    public async Task RejectsDtd() =>
        await Assert.ThrowsAsync<InvalidDataException>(() => Capture("<!DOCTYPE TestRun [<!ENTITY x SYSTEM 'file:///private'>]><TestRun>&x;</TestRun>"));

    [Fact]
    public async Task RejectsNonTrxDocument() =>
        await Assert.ThrowsAsync<InvalidDataException>(() => Capture("<results />"));

    [Fact]
    public async Task CancellationDoesNotReturnSuccessfulCapture() =>
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Capture(TrxFixtures.Run(), new CancellationToken(true)));

    [Theory]
    [InlineData("Error")]
    [InlineData("Aborted")]
    [InlineData("Unknown")]
    public async Task RunSummaryErrorPreventsPassingExecution(string outcome)
    {
        var xml = TrxFixtures.Run("Passed").Replace("outcome=\"Completed\"", $"outcome=\"{outcome}\"");
        var run = await Capture(xml);
        Assert.Equal(ExecutionStatus.Inconclusive, run.Execution.Status);
        Assert.False(run.Execution.IsComplete);
    }

    [Fact]
    public async Task TestHostErrorPreventsPassingExecution()
    {
        var xml = TrxFixtures.Run("Passed").Replace("</ResultSummary>", "<RunInfos><RunInfo outcome=\"Error\"><Text>sensitive diagnostic</Text></RunInfo></RunInfos></ResultSummary>");
        var run = await Capture(xml);
        Assert.Equal(ExecutionStatus.Inconclusive, run.Execution.Status);
        Assert.False(run.Execution.IsComplete);
    }
}
