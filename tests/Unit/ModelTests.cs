using PulseGrid.Core;
using Xunit;

namespace PulseGrid.Tests.Unit;

public class ModelTests
{
    [Fact]
    public void ExecutionRejectsEmptyId() =>
        Assert.Throws<ArgumentException>(() => new Execution(Guid.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ExecutionStatus.Passed, true));

    [Fact]
    public void ExecutionRejectsNonUtcTimestamp() =>
        Assert.Throws<ArgumentException>(() => new Execution(Guid.NewGuid(), DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(2)), DateTimeOffset.UtcNow, ExecutionStatus.Passed, true));

    [Fact]
    public void IncompleteExecutionCannotPass() =>
        Assert.Throws<ArgumentException>(() => new Execution(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ExecutionStatus.Passed, false));

}

