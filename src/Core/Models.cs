using System.Text.Json.Serialization;

namespace PulseGrid.Core;

/// <summary>The observed outcome of a test execution, never a claim of software correctness.</summary>
public enum ExecutionStatus
{
    /// <summary>All expected tests passed and evidence is complete.</summary>
    Passed,
    /// <summary>At least one test failed.</summary>
    Failed,
    /// <summary>Results are missing, skipped, unknown, or incomplete.</summary>
    Inconclusive
}

/// <summary>An individual observed test outcome.</summary>
public enum TestOutcome
{
    /// <summary>The test passed.</summary>
    Passed,
    /// <summary>The test failed.</summary>
    Failed,
    /// <summary>The test was not executed.</summary>
    Skipped,
    /// <summary>The outcome could not be established.</summary>
    Inconclusive
}

/// <summary>Immutable metadata for an observed test execution.</summary>
public sealed record Execution
{
    /// <summary>Creates validated execution metadata using UTC timestamps.</summary>
    [JsonConstructor]
    public Execution(Guid id, DateTimeOffset startedAt, DateTimeOffset completedAt, ExecutionStatus status, bool isComplete, string? sourceRevision = null, string? branch = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Execution ID must not be empty.", nameof(id));
        if (startedAt.Offset != TimeSpan.Zero || completedAt.Offset != TimeSpan.Zero)
            throw new ArgumentException("Execution timestamps must be UTC.");
        if (completedAt < startedAt) throw new ArgumentException("Completion precedes the start.");
        if (!Enum.IsDefined(status)) throw new ArgumentException("Unknown execution status.", nameof(status));
        if (status == ExecutionStatus.Passed && !isComplete)
            throw new ArgumentException("Incomplete evidence cannot establish a passing execution.");
        Id = id;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Status = status;
        IsComplete = isComplete;
        SourceRevision = sourceRevision;
        Branch = branch;
    }

    /// <summary>The stable execution identifier.</summary>
    public Guid Id { get; }
    /// <summary>The execution category.</summary>
    public string Type => "test";
    /// <summary>The UTC execution start.</summary>
    public DateTimeOffset StartedAt { get; }
    /// <summary>The UTC execution completion.</summary>
    public DateTimeOffset CompletedAt { get; }
    /// <summary>The observed outcome.</summary>
    public ExecutionStatus Status { get; }
    /// <summary>Whether the adapter observed all expected results.</summary>
    public bool IsComplete { get; }
    /// <summary>An explicitly supplied source revision.</summary>
    public string? SourceRevision { get; }
    /// <summary>An explicitly supplied branch.</summary>
    public string? Branch { get; }
}

/// <summary>A sanitized test result; display parameters and diagnostics are excluded.</summary>
/// <param name="Id">Result identifier from the test runner.</param>
/// <param name="Method">Stable class and method identifier, without display parameters.</param>
/// <param name="Outcome">Observed outcome.</param>
/// <param name="DurationMilliseconds">Reported duration, or null when unavailable.</param>
/// <param name="Diagnostics">Redaction marker for failure diagnostics.</param>
public sealed record TestResult(Guid Id, string Method, TestOutcome Outcome, double? DurationMilliseconds, string? Diagnostics);

/// <summary>A captured execution and its sanitized test results.</summary>
/// <param name="Execution">Observed execution metadata.</param>
/// <param name="Tests">The sanitized results.</param>
public sealed record CapturedRun(Execution Execution, IReadOnlyList<TestResult> Tests);
