using Ocelot.Logging;
using Ocelot.Provider.Kubernetes;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ocelot.UnitTests.Kubernetes;

[Trait("Feat", "345")] // https://github.com/ThreeMammals/Ocelot/issues/345
public sealed class PollKubeTests : UnitTest, IDisposable
{
    private PollKube _provider;
    private readonly Mock<IOcelotLoggerFactory> _factory = new();
    private readonly Mock<IOcelotLogger> _logger = new();
    private readonly Mock<IServiceDiscoveryProvider> _discoveryProvider = new();

    const int PollingIntervalMs = 1;

    public PollKubeTests()
    {
        _factory.Setup(x => x.CreateLogger<PollKube>()).Returns(_logger.Object);
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }

    [Fact]
    public void Dispose_Manually()
    {
        var instance = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        instance.Dispose();
    }

    [Fact]
    [Trait("PR", "772")] // https://github.com/ThreeMammals/Ocelot/pull/772
    public void Should_return_service_from_kube()
    {
        // Arrange
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        List<Service> services = [service];
        _discoveryProvider.Setup(x => x.GetAsync()).ReturnsAsync(services);
        _provider = new PollKube(PollingIntervalMs, _factory.Object, _discoveryProvider.Object);

        // Act
        var actual = WhenIGetTheServices(1);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(1, actual.Count);
    }

    private List<Service> WhenIGetTheServices(int expected)
    {
        List<Service> services = null;
        var result = Wait.For(3_000).Until(() =>
        {
            try
            {
                services = _provider.GetAsync().GetAwaiter().GetResult();
                return services.Count == expected;
            }
            catch (Exception)
            {
                return false;
            }
        });
        Assert.True(result);
        return services;
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task OnTimerCallbackAsync_AvoidPolling_WhenAlreadyPolling()
    {
        // Arrange
        int pollingInterval = 100;
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        List<Service> services = [service];
        var slowPolling = Task.Delay(pollingInterval + 50).ContinueWith(x => services);
        _discoveryProvider.Setup(x => x.GetAsync()).Returns(slowPolling);
        _provider = new PollKube(pollingInterval, _factory.Object, _discoveryProvider.Object);

        // Act
        var coldRequestTask = _provider.GetAsync(); // calls Poll() due to empty queue
        var method = _provider.GetType().GetMethod("OnTimerCallbackAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(_provider, [new object()]);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Once);

        var actual = await coldRequestTask;
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Once);

        method.Invoke(_provider, [new object()]);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Exactly(2));
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task GetAsync()
    {
        // Arrange
        int pollingInterval = 100;
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        List<Service> services = [service];
        var slowPolling = Task.Delay(pollingInterval + 50).ContinueWith(x => services);
        _discoveryProvider.Setup(x => x.GetAsync()).Returns(slowPolling);
        _provider = new PollKube(pollingInterval, _factory.Object, _discoveryProvider.Object);

        FieldInfo pollingField = _provider.GetType().GetField("_polling", BindingFlags.Instance | BindingFlags.NonPublic);
        pollingField.SetValue(_provider, true);
        FieldInfo queueField = _provider.GetType().GetField("_queue", BindingFlags.Instance | BindingFlags.NonPublic);
        var queue = queueField.GetValue(_provider) as ConcurrentQueue<List<Service>>;
        List<Service> oldVersion = [service];
        queue.Enqueue(oldVersion);

        // Act
        var actual = await _provider.GetAsync(); // will NOT call Poll()
        Assert.Same(oldVersion, actual);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Never);

        // Scenario 2: For services with multiple versions, remove outdated versions and retain only the latest one
        pollingField.SetValue(_provider, false);
        List<Service> latestVersion = [new Service("", new("h", 123), "", "", default)];
        queue.Enqueue(latestVersion);
        Assert.Equal(2, queue.Count);

        actual = await _provider.GetAsync(); // will NOT call Poll()
        Assert.Equal(1, queue.Count);
        Assert.Same(latestVersion, actual);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Never);
    }
}
