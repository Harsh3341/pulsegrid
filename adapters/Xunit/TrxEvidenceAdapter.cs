using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PulseGrid.Abstractions;
using PulseGrid.Core;

namespace PulseGrid.Evidence.Xunit;

/// <summary>Ingests xUnit results produced by the dotnet test TRX logger; diagnostics are excluded.</summary>
public sealed class TrxEvidenceAdapter : IEvidenceAdapter
{
    private const string Namespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private const int MaxTests = 10_000;

    /// <inheritdoc />
    public string Name => "pulsegrid.xunit.trx";

    /// <inheritdoc />
    public async Task<CapturedRun> CaptureAsync(Stream input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = 64 * 1024 * 1024, CloseInput = false
            });
            await reader.MoveToContentAsync();
            if (reader.LocalName != "TestRun" || reader.NamespaceURI != Namespace)
                throw new InvalidDataException("Expected a Visual Studio TRX TestRun.");
            var runId = ParseId(reader.GetAttribute("id"));
            DateTimeOffset? start = null, finish = null;
            int? expectedTotal = null, expectedPassed = null, expectedFailed = null;
            int? expectedExecuted = null;
            string? summaryOutcome = null;
            var sawSummary = false;
            var runProblem = false;
            var methods = new Dictionary<Guid, string>();
            var results = new List<(Guid Id, Guid TestId, TestOutcome Outcome, double? Duration)>();
            var resultIds = new HashSet<Guid>();
            while (!reader.EOF)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == Namespace)
                {
                    if (reader.LocalName == "Times")
                    {
                        if (start is not null) throw new InvalidDataException("Duplicate execution times.");
                        start = ParseTime(reader.GetAttribute("start"));
                        finish = ParseTime(reader.GetAttribute("finish"));
                    }
                    else if (reader.LocalName == "ResultSummary")
                    {
                        if (sawSummary) throw new InvalidDataException("Duplicate result summary.");
                        sawSummary = true;
                        summaryOutcome = reader.GetAttribute("outcome");
                    }
                    else if (reader.LocalName == "RunInfo")
                    {
                        var outcome = reader.GetAttribute("outcome");
                        if (outcome is not ("Completed" or "Passed" or "Warning")) runProblem = true;
                    }
                    else if (reader.LocalName == "Counters")
                    {
                        if (expectedTotal is not null) throw new InvalidDataException("Duplicate result counters.");
                        expectedTotal = ParseCount(reader.GetAttribute("total"));
                        expectedPassed = ParseCount(reader.GetAttribute("passed"));
                        expectedFailed = ParseCount(reader.GetAttribute("failed"));
                        expectedExecuted = ParseCount(reader.GetAttribute("executed"));
                        foreach (var counter in new[] { "error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable", "disconnected", "inProgress", "pending" })
                            if (reader.GetAttribute(counter) is { } value && ParseCount(value) != 0) runProblem = true;
                    }
                    else if (reader.LocalName == "UnitTestResult")
                    {
                        if (results.Count >= MaxTests) throw new InvalidDataException("TRX exceeds 10,000 results.");
                        var id = ParseId(reader.GetAttribute("executionId"));
                        if (!resultIds.Add(id)) throw new InvalidDataException("Duplicate test execution ID.");
                        var outcome = reader.GetAttribute("outcome") switch
                        {
                            "Passed" => TestOutcome.Passed,
                            "Failed" => TestOutcome.Failed,
                            "NotExecuted" => TestOutcome.Skipped,
                            _ => TestOutcome.Inconclusive
                        };
                        double? duration = TimeSpan.TryParse(reader.GetAttribute("duration"), CultureInfo.InvariantCulture, out var parsed) && parsed >= TimeSpan.Zero
                            ? parsed.TotalMilliseconds : null;
                        results.Add((id, ParseId(reader.GetAttribute("testId")), outcome, duration));
                    }
                    else if (reader.LocalName == "UnitTest")
                    {
                        if (methods.Count >= MaxTests) throw new InvalidDataException("TRX exceeds 10,000 definitions.");
                        var element = (XElement)await XNode.ReadFromAsync(reader, cancellationToken);
                        var method = element.Element(XName.Get("TestMethod", Namespace));
                        var className = ((string?)method?.Attribute("className"))?.Split(',')[0].Trim();
                        var name = (string?)method?.Attribute("name");
                        var stableName = SafeClass(className) && SafeMethod(name) ? className + "." + name : "[unavailable]";
                        if (!methods.TryAdd(ParseId((string?)element.Attribute("id")), stableName))
                            throw new InvalidDataException("Duplicate test definition ID.");
                        continue;
                    }
                }
                await reader.ReadAsync();
            }
            if (start is null || finish is null || finish < start)
                throw new InvalidDataException("TRX requires valid start and finish times.");
            var tests = results.Select(result => new TestResult(result.Id,
                methods.GetValueOrDefault(result.TestId, "[unavailable]"),
                result.Outcome, result.Duration, result.Outcome == TestOutcome.Failed ? "[REDACTED]" : null)).ToArray();
            var complete = tests.Length > 0 && expectedTotal == tests.Length
                && expectedExecuted == tests.Count(t => t.Outcome != TestOutcome.Skipped)
                && !runProblem && (summaryOutcome == "Completed" || summaryOutcome == "Failed" && tests.Any(t => t.Outcome == TestOutcome.Failed))
                && expectedPassed == tests.Count(t => t.Outcome == TestOutcome.Passed)
                && expectedFailed == tests.Count(t => t.Outcome == TestOutcome.Failed)
                && tests.All(t => t.Outcome != TestOutcome.Inconclusive && t.Method != "[unavailable]");
            var status = tests.Any(t => t.Outcome == TestOutcome.Failed) ? ExecutionStatus.Failed
                : complete && tests.All(t => t.Outcome == TestOutcome.Passed) ? ExecutionStatus.Passed
                : ExecutionStatus.Inconclusive;
            return new CapturedRun(new Execution(runId, start.Value, finish.Value, status, complete), tests);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("Malformed or unsafe TRX XML.", ex);
        }
    }

    private static Guid ParseId(string? value) =>
        Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new InvalidDataException("TRX contains an invalid identifier.");

    private static DateTimeOffset ParseTime(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            && value is not null && Regex.IsMatch(value, @"(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))
            ? time.ToUniversalTime() : throw new InvalidDataException("TRX contains an invalid timestamp.");

    private static int ParseCount(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count >= 0 && count <= MaxTests
            ? count : throw new InvalidDataException("TRX contains an invalid result count.");

    private static bool SafeClass(string? value) => value is { Length: > 0 and <= 512 }
        && Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_.+`]*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static bool SafeMethod(string? value) => value is { Length: > 0 and <= 256 }
        && Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
