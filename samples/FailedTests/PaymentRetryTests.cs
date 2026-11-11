using Xunit;

namespace PulseGrid.Samples.FailedTests;

// This executable fixture deliberately demonstrates a failure. It is outside the solution.
public class PaymentRetryTests
{
    [Fact]
    public void RetryAfterTimeoutShouldNotChargeTwice()
    {
        var provider = new PaymentProviderSimulator();
        try { provider.Capture(simulateTimeoutAfterCapture: true); }
        catch (TimeoutException) { provider.Capture(simulateTimeoutAfterCapture: false); }
        Assert.Equal(1, provider.Captures);
    }

    private sealed class PaymentProviderSimulator
    {
        public int Captures { get; private set; }
        public void Capture(bool simulateTimeoutAfterCapture)
        {
            Captures++;
            if (simulateTimeoutAfterCapture) throw new TimeoutException("password=sample-sensitive-value");
        }
    }
}
