using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Creator;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Ocelot.UnitTests.Responder;

namespace Ocelot.UnitTests.Requester;

public class DelegatingHandlerHandlerProviderFactoryTests : UnitTest
{
    private DelegatingHandlerHandlerFactory _factory;
    private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
    private readonly Mock<IOcelotLogger> _logger;
    private readonly Mock<IQoSFactory> _qosFactory;
    private readonly Mock<ITracingHandlerFactory> _tracingFactory;
    private IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;
    private readonly QosDelegatingHandlerDelegate _qosDelegate;

    public DelegatingHandlerHandlerProviderFactoryTests()
    {
        _qosDelegate = (a, b, c) => new FakeQoSHandler();
        _tracingFactory = new Mock<ITracingHandlerFactory>();
        _qosFactory = new Mock<IQoSFactory>();
        _loggerFactory = new Mock<IOcelotLoggerFactory>();
        _logger = new Mock<IOcelotLogger>();
        _loggerFactory.Setup(x => x.CreateLogger<DelegatingHandlerHandlerFactory>()).Returns(_logger.Object);
        _services = new ServiceCollection();
        _services.AddSingleton(_qosDelegate);
    }

    [Fact]
    public void Should_follow_ordering_add_specifics()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithDelegatingHandlers(new List<string>
            {
                "FakeDelegatingHandler",
                "FakeDelegatingHandlerTwo",
            })
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(6);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(2);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(3);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(4);
        result.ThenHandlerAtPositionIs<FakeQoSHandler>(5);
    }

    [Fact]
    public void Should_follow_ordering_order_specifics_and_globals()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithDelegatingHandlers(new List<string>
            {
                "FakeDelegatingHandlerTwo",
                "FakeDelegatingHandler",
                "FakeDelegatingHandlerFour",
            })
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerFour, FakeDelegatingHandlerThree>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(6);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0); //first because global not in config
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1); //first from config
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(2); //second from config
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(3); //third from config (global)
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(4);
        result.ThenHandlerAtPositionIs<FakeQoSHandler>(5);
    }

    [Fact]
    public void Should_follow_ordering_order_specifics()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithDelegatingHandlers(new List<string>
            {
                "FakeDelegatingHandlerTwo",
                "FakeDelegatingHandler",
            })
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(6);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(2);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(3);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(4);
        result.ThenHandlerAtPositionIs<FakeQoSHandler>(5);
    }

    [Fact]
    public void Should_follow_ordering_order_and_only_add_specifics_in_config()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithDelegatingHandlers(new List<string>
            {
                "FakeDelegatingHandler",
            })
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(5);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(2);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(3);
        result.ThenHandlerAtPositionIs<FakeQoSHandler>(4);
    }

    [Fact]
    public void Should_follow_ordering_dont_add_specifics()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(4);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(2);
        result.ThenHandlerAtPositionIs<FakeQoSHandler>(3);
    }

    [Fact]
    public void Should_apply_re_route_specific()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithDelegatingHandlers(new List<string>
            {
                "FakeDelegatingHandler",
                "FakeDelegatingHandlerTwo",
            })
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(2);
        result.ThenTheDelegatesAreAddedCorrectly();
    }

    [Fact]
    public void Should_all_from_all_routes_provider_and_qos()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(3);
        result.ThenTheDelegatesAreAddedCorrectly();
        result.ThenItIsQosHandler(2);
    }

    [Fact]
    public void Should_return_provider_with_no_delegates()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheServiceProviderReturnsNothing();

        // Act
        var result = WhenIGet(route);

        // Assert: Then No Delegates Are In The Provider
        result.ShouldNotBeNull();
        result.Data.Count.ShouldBe(0);
    }

    [Fact]
    public void Should_return_provider_with_qos_delegate()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheServiceProviderReturnsNothing();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(1);
        result.ThenItIsQosHandler(0);
    }

    [Fact]
    public void Should_return_provider_with_qos_delegate_when_timeout_value_set()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        GivenTheQosFactoryReturns(new FakeQoSHandler());
        GivenTheServiceProviderReturnsNothing();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(1);
        result.ThenItIsQosHandler(0);
    }

    [Fact]
    public void Should_log_error_and_return_no_qos_provider_delegate_when_qos_factory_returns_error()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        _qosFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new ErrorResponse<DelegatingHandler>(new AnyError()));
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(4);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(2);
        result.ThenHandlerAtPositionIs<NoQosDelegatingHandler>(3);
        ThenTheWarningIsLogged(route);
    }

    [Fact]
    public void Should_log_error_and_return_no_qos_provider_delegate_when_qos_factory_returns_null()
    {
        // Arrange
        var qosOptions = new QoSOptionsBuilder()
            .WithTimeoutValue(1)
            .WithDurationOfBreak(1)
            .WithExceptionsAllowedBeforeBreaking(1)
            .Build();
        var route = new DownstreamRouteBuilder()
            .WithQosOptions(qosOptions)
            .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true, true, int.MaxValue, DefaultPooledConnectionLifeTime))
            .WithLoadBalancerKey(string.Empty)
            .Build();
        _qosFactory.Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns((ErrorResponse<DelegatingHandler>)null);
        GivenTheTracingFactoryReturns();
        GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();
        GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>();

        // Act
        var result = WhenIGet(route);

        // Assert
        result.ThenThereIsDelegatesInProvider(4);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandler>(0);
        result.ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1);
        result.ThenHandlerAtPositionIs<FakeTracingHandler>(2);
        result.ThenHandlerAtPositionIs<NoQosDelegatingHandler>(3);
        ThenTheWarningIsLogged(route);
    }

    private void ThenTheWarningIsLogged(DownstreamRoute route)
    {
        _logger.Verify(x => x.LogWarning(It.Is<Func<string>>(y => y.Invoke() == $"Route {route.UpstreamPathTemplate} specifies use QoS but no QosHandler found in DI container. Will use not use a QosHandler, please check your setup!")), Times.Once);
    }

    private void GivenTheTracingFactoryReturns()
    {
        _tracingFactory
            .Setup(x => x.Get())
            .Returns(new FakeTracingHandler());
    }

    private void GivenTheServiceProviderReturnsGlobalDelegatingHandlers<TOne, TTwo>()
        where TOne : DelegatingHandler
        where TTwo : DelegatingHandler
    {
        _services.AddTransient<TOne>();
        _services.AddTransient(s =>
        {
            var service = s.GetService<TOne>();
            return new GlobalDelegatingHandler(service);
        });
        _services.AddTransient<TTwo>();
        _services.AddTransient(s =>
        {
            var service = s.GetService<TTwo>();
            return new GlobalDelegatingHandler(service);
        });
    }

    private void GivenTheServiceProviderReturnsSpecificDelegatingHandlers<TOne, TTwo>()
        where TOne : DelegatingHandler
        where TTwo : DelegatingHandler
    {
        _services.AddTransient<DelegatingHandler, TOne>();
        _services.AddTransient<DelegatingHandler, TTwo>();
    }

    private void GivenTheServiceProviderReturnsNothing()
    {
        _serviceProvider = _services.BuildServiceProvider(true);
    }

    private void GivenTheQosFactoryReturns(DelegatingHandler handler)
    {
        _qosFactory
            .Setup(x => x.Get(It.IsAny<DownstreamRoute>()))
            .Returns(new OkResponse<DelegatingHandler>(handler));
    }

    private Response<List<Func<DelegatingHandler>>> WhenIGet(DownstreamRoute route)
    {
        _serviceProvider = _services.BuildServiceProvider(true);
        _factory = new DelegatingHandlerHandlerFactory(_tracingFactory.Object, _qosFactory.Object, _serviceProvider, _loggerFactory.Object);
        return _factory.Get(route);
    }

    /// <summary>120 seconds.</summary>
    private static TimeSpan DefaultPooledConnectionLifeTime => TimeSpan.FromSeconds(HttpHandlerOptionsCreator.DefaultPooledConnectionLifetimeSeconds);
}

internal static class ResponseExtensions
{
    public static void ThenItIsQosHandler(this Response<List<Func<DelegatingHandler>>> result, int i)
    {
        var delegates = result.Data;
        var del = delegates[i].Invoke();
        del.ShouldBeOfType<FakeQoSHandler>();
    }

    public static void ThenTheDelegatesAreAddedCorrectly(this Response<List<Func<DelegatingHandler>>> result)
    {
        var delegates = result.Data;

        var del = delegates[0].Invoke();
        var handler = (FakeDelegatingHandler)del;
        handler.Order.ShouldBe(1);

        del = delegates[1].Invoke();
        var handlerTwo = (FakeDelegatingHandlerTwo)del;
        handlerTwo.Order.ShouldBe(2);
    }

    public static void ThenThereIsDelegatesInProvider(this Response<List<Func<DelegatingHandler>>> result, int count)
    {
        result.ShouldNotBeNull();
        result.Data.Count.ShouldBe(count);
    }

    public static void ThenHandlerAtPositionIs<T>(this Response<List<Func<DelegatingHandler>>> result, int pos)
        where T : DelegatingHandler
    {
        var delegates = result.Data;
        var del = delegates[pos].Invoke();
        del.ShouldBeOfType<T>();
    }
}

internal class FakeTracingHandler : DelegatingHandler, ITracingHandler
{
}

internal class FakeQoSHandler : DelegatingHandler
{
}
