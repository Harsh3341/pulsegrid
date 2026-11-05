namespace PulseGrid.Tests;

internal static class TrxFixtures
{
    public static string Run(string outcome = "Failed", int total = 1, string method = "Retries", string diagnostics = "password=do-not-store")
    {
        var failed = outcome == "Failed" ? 1 : 0;
        var passed = outcome == "Passed" ? 1 : 0;
        var executed = outcome == "NotExecuted" ? 0 : 1;
        var summary = outcome == "Failed" ? "Failed" : "Completed";
        return $"""
        <TestRun id="00000000-0000-0000-0000-000000000001" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Times start="2026-11-15T08:00:00Z" finish="2026-11-15T08:00:01Z" />
          <Results><UnitTestResult executionId="00000000-0000-0000-0000-000000000002"
            testId="00000000-0000-0000-0000-000000000003" testName="Retries(token=secret-display)"
            outcome="{outcome}" duration="00:00:00.125">
            <Output><ErrorInfo><Message>{diagnostics}</Message><StackTrace>secret-stack</StackTrace></ErrorInfo><StdOut>secret-output</StdOut></Output>
          </UnitTestResult></Results>
          <TestDefinitions><UnitTest id="00000000-0000-0000-0000-000000000003">
            <TestMethod className="Demo.PaymentTests" name="{method}" />
          </UnitTest></TestDefinitions>
          <ResultSummary outcome="{summary}"><Counters total="{total}" executed="{executed}" passed="{passed}" failed="{failed}" /></ResultSummary>
        </TestRun>
        """;
    }
}
