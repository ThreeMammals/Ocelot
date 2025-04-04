using System.Runtime.InteropServices;

namespace Ocelot.UnitTests;

/// <summary>
/// Apply <see cref="CollectionAttribute"/> to classes to disable parallelization.
/// </summary>
[CollectionDefinition(nameof(SequentialTests), DisableParallelization = true)]
public class SequentialTests
{
    /// <summary>
    /// Unstable <see cref="RateLimiting.RateLimitingTests"/>.
    /// </summary>
    [Collection(nameof(SequentialTests))]
    public class RateLimitingTests
    {
        [Fact]
        public void ProcessRequest_PeriodTimespanValueIsGreaterThanPeriod_ExpectedBehaviorAndExpirationInPeriod()
        {
            // Skip in MacOS because the test is very unstable.
            // The test is stable in Linux and Windows only
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Test #1
                var test = new RateLimiting.RateLimitingTests();
                test.ProcessRequest_PeriodTimespanValueIsGreaterThanPeriod_ExpectedBehaviorAndExpirationInPeriod();
            }
        }
    }

    /// <summary>
    /// Unstable <see cref="Requester.MessageInvokerPoolTests"/>.
    /// </summary>
    [Collection(nameof(SequentialTests))]
    public class MessageInvokerPoolTests
    {
        [Fact]
        public async Task Should_reuse_cookies_from_container()
        {
            // Test #1
            var test = new Requester.MessageInvokerPoolTests();
            await test.Should_reuse_cookies_from_container();
        }
    }

    /// <summary>
    /// Unstable <see cref="Consul.ConsulTests"/>.
    /// </summary>
    [Collection(nameof(SequentialTests))]
    public class ConsulTests : Consul.ConsulTests
    { } // all tests

    /// <summary>
    /// Unstable <see cref="Kubernetes.KubeTests"/>.
    /// </summary>
    [Collection(nameof(SequentialTests))]
    public class KubeTests : Kubernetes.KubeTests
    { } // all tests
}
