using PulseGrid.Specifications;
using Xunit;

namespace PulseGrid.Tests.Unit;

public class PayloadTests
{
    [Theory]
    [InlineData("[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"method\":\"Demo.Tests.Run\"}]")]
    [InlineData("[{\"id\":\"10000000-0000-0000-0000-000000000001\",\"method\":\"Demo.Tests.Run\",\"outcome\":\"passed\",\"durationMilliseconds\":null,\"diagnostics\":\"secret\"}]")]
    public void MissingOutcomeAndUnredactedDiagnosticsAreRejected(string json) =>
        Assert.Throws<InvalidDataException>(() => TestResultsCodec.Deserialize(System.Text.Encoding.UTF8.GetBytes(json)));
}
