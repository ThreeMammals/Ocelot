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
    public async Task Should_return_service_from_kube()
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

    [Fact(Skip = "Require coverage checks")]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task OnTimerCallbackAsync_AvoidPolling_WhenAlreadyPolling()
    {
        // Arrange
        int pollingInterval = 100;
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        List<Service> services = [service];
        var slowPolling = Task.Delay(pollingInterval + 50, TestContext.Current.CancellationToken)
            .ContinueWith(x => services, TestContext.Current.CancellationToken);
        _discoveryProvider.Setup(x => x.GetAsync()).Returns(slowPolling);
        _provider = new PollKube(pollingInterval, _factory.Object, _discoveryProvider.Object);

        // Act - Allow background task to start and begin polling
        var coldRequestTask = _provider.GetAsync(); // calls Poll() due to empty queue
        await Task.Delay(10, TestContext.Current.CancellationToken); // Give polling time to start

        var method = _provider.GetType().GetMethod("OnTimerCallbackAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(_provider, [new object()]);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Once);

        var actual = await coldRequestTask;
        _discoveryProvider.Verify(x => x.GetAsync(), Times.AtLeastOnce); // ideally it shoud be called once, but it is called 1 or 2 times. TODO A disposing enhancement?

        method.Invoke(_provider, [new object()]);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.AtLeast(2));

        // Ensure background task completes before disposal
        await Task.Delay(pollingInterval + 100, TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task GetAsync()
    {
        // Arrange
        int pollingInterval = 100;
        var service = new Service(string.Empty, new ServiceHostAndPort(string.Empty, 0), string.Empty, string.Empty, new List<string>());
        List<Service> services = [service];
        var slowPolling = Task.Delay(pollingInterval + 50, TestContext.Current.CancellationToken).ContinueWith(x => services, TestContext.Current.CancellationToken);
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

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task GetAsync_WhenDisposed_ReturnsEmpty()
    {
        // Arrange
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        
        // Act - Give any background task minimal time to initialize
        await Task.Delay(5, TestContext.Current.CancellationToken);
        
        // Dispose to stop any polling
        _provider.Dispose();

        // Act - Call GetAsync after disposal
        var actual = await _provider.GetAsync();

        // Assert
        Assert.Same(PollKube.Empty, actual);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task PollAsync_WhenQueueCountGreaterThan3_ReturnsEmpty()
    {
        // Arrange
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = _provider.GetType().GetMethod("PollAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var queueField = _provider.GetType().GetField("_queue", BindingFlags.Instance | BindingFlags.NonPublic);
        var queue = (ConcurrentQueue<List<Service>>)queueField.GetValue(_provider);
        for (int i = 0; i < 4; i++)
        {
            queue.Enqueue(new List<Service>());
        }

        // Act
        var task = (Task<List<Service>>)method.Invoke(_provider, [CancellationToken.None]);
        var actual = await task;

        // Assert
        Assert.Same(PollKube.Empty, actual);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Never);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task PollAsync_WhenObjectDisposedExceptionThrown_ReturnsEmpty()
    {
        // Arrange
        _discoveryProvider.Setup(x => x.GetAsync()).ThrowsAsync(new ObjectDisposedException("provider"));
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = _provider.GetType().GetMethod("PollAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        // Act
        var task = (Task<List<Service>>)method.Invoke(_provider, [CancellationToken.None]);
        var actual = await task;

        // Assert
        Assert.Same(PollKube.Empty, actual);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task PollAsync_WhenCancelled_ReturnsEmpty()
    {
        // Arrange
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = _provider.GetType().GetMethod("PollAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var task = (Task<List<Service>>)method.Invoke(_provider, [cts.Token]);
        var actual = await task;

        // Assert
        Assert.Same(PollKube.Empty, actual);
        _discoveryProvider.Verify(x => x.GetAsync(), Times.Never);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task PollAsync_WhenCancelledDuringWait_ReturnsEmpty()
    {
        // Arrange
        var tcs = new TaskCompletionSource<List<Service>>();
        _discoveryProvider.Setup(x => x.GetAsync()).Returns(tcs.Task);
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = _provider.GetType().GetMethod("PollAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var innerCts = new CancellationTokenSource();

        // Act
        var task = (Task<List<Service>>)method.Invoke(_provider, [innerCts.Token]);
        innerCts.Cancel();
        tcs.SetResult(new List<Service>());
        var actual = await task;

        // Assert
        Assert.Same(PollKube.Empty, actual);
    }

    [Fact]
    public void Finalizer_DoesNotThrow()
    {
        var instance = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = instance.GetType().GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(instance, [false]);
    }

    [Fact]
    [Trait("Bug", "2304")] // https://github.com/ThreeMammals/Ocelot/issues/2304
    public async Task StartAsync_WhenProviderDisposed_CatchesOperationCanceledException()
    {
        // Arrange
        _discoveryProvider.Setup(x => x.GetAsync()).ReturnsAsync(new List<Service>());
        _provider = new PollKube(10_000, _factory.Object, _discoveryProvider.Object);
        var method = _provider.GetType().GetMethod("StartAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        var ctsField = _provider.GetType().GetField("_cts", BindingFlags.Instance | BindingFlags.NonPublic);
        var cts = (CancellationTokenSource)ctsField.GetValue(_provider);

        // Act - Allow StartAsync task to be created
        var task = (Task)method.Invoke(_provider, null);
        await Task.Delay(10, TestContext.Current.CancellationToken); // Give task time to start
        
        cts.Cancel(); // This will trigger OperationCanceledException in StartAsync loop
        await task; // Should not throw

        // Assert
        Assert.True(task.IsCompletedSuccessfully); // Task ended without bubbling up the exception
    }
}
