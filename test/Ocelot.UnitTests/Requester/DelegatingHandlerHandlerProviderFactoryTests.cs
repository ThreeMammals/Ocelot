using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class DelegatingHandlerHandlerProviderFactoryTests
    {
        private DelegatingHandlerHandlerFactory _factory;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private DownstreamReRoute _request;
        private Response<List<Func<DelegatingHandler>>> _result;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly Mock<ITracingHandlerFactory> _tracingFactory;
        private IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;

        public DelegatingHandlerHandlerProviderFactoryTests()
        {
            _tracingFactory = new Mock<ITracingHandlerFactory>();
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _services = new ServiceCollection();
        }

        [Fact]
        public void should_follow_ordering_add_specifics()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                .WithDelegatingHandlers(new List<string>
                {
                    "FakeDelegatingHandler",
                    "FakeDelegatingHandlerTwo"
                })
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheTracingFactoryReturns())
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>())
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(6))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandler>(2))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(3))
                .And(x => ThenHandlerAtPositionIs<FakeTracingHandler>(4))
                .And(x => ThenHandlerAtPositionIs<PollyCircuitBreakingDelegatingHandler>(5))
                .BDDfy();
        }

        [Fact]
        public void should_follow_ordering_order_specifics_and_globals()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                .WithDelegatingHandlers(new List<string>
                {
                    "FakeDelegatingHandlerTwo",
                    "FakeDelegatingHandler",
                    "FakeDelegatingHandlerFour"
                })
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheTracingFactoryReturns())
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerFour, FakeDelegatingHandlerThree>())
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(6))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0)) //first because global not in config
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1)) //first from config
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandler>(2)) //second from config
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(3)) //third from config (global)
                .And(x => ThenHandlerAtPositionIs<FakeTracingHandler>(4))
                .And(x => ThenHandlerAtPositionIs<PollyCircuitBreakingDelegatingHandler>(5))
                .BDDfy();
        }

        [Fact]
        public void should_follow_ordering_order_specifics()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                .WithDelegatingHandlers(new List<string>
                {
                    "FakeDelegatingHandlerTwo",
                    "FakeDelegatingHandler"
                })
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheTracingFactoryReturns())
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>())
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(6))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(2))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandler>(3))
                .And(x => ThenHandlerAtPositionIs<FakeTracingHandler>(4))
                .And(x => ThenHandlerAtPositionIs<PollyCircuitBreakingDelegatingHandler>(5))
                .BDDfy();
        }

        [Fact]
        public void should_follow_ordering_order_and_only_add_specifics_in_config()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                .WithDelegatingHandlers(new List<string>
                {
                    "FakeDelegatingHandler",
                })
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheTracingFactoryReturns())
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandlerThree, FakeDelegatingHandlerFour>())
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(5))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerThree>(0))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerFour>(1))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandler>(2))
                .And(x => ThenHandlerAtPositionIs<FakeTracingHandler>(3))
                .And(x => ThenHandlerAtPositionIs<PollyCircuitBreakingDelegatingHandler>(4))
                .BDDfy();
        }

        [Fact]
        public void should_follow_ordering_dont_add_specifics()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, true))
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheTracingFactoryReturns())
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(4))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandler>(0))
                .And(x => ThenHandlerAtPositionIs<FakeDelegatingHandlerTwo>(1))
                .And(x => ThenHandlerAtPositionIs<FakeTracingHandler>(2))
                .And(x => ThenHandlerAtPositionIs<PollyCircuitBreakingDelegatingHandler>(3))
                .BDDfy();
        }

        [Fact]
        public void should_apply_re_route_specific()
        {
            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false))
                .WithDelegatingHandlers(new List<string>
                {
                    "FakeDelegatingHandler",
                    "FakeDelegatingHandlerTwo"
                })
                .WithLoadBalancerKey("")
                .Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsSpecificDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(2))
                .And(x => ThenTheDelegatesAreAddedCorrectly())
                .BDDfy();
        }

        [Fact]
        public void should_all_from_all_routes_provider_and_qos()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithLoadBalancerKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsGlobalDelegatingHandlers<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(3))
                .And(x => ThenTheDelegatesAreAddedCorrectly())
                .And(x => ThenItIsPolly(2))
                .BDDfy(); 
        }

        [Fact]
        public void should_return_provider_with_no_delegates()
        {
            var qosOptions = new QoSOptionsBuilder()
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithLoadBalancerKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheServiceProviderReturnsNothing())
                .When(x => WhenIGet())
                .Then(x => ThenNoDelegatesAreInTheProvider())
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_qos_delegate()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithLoadBalancerKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturnsNothing())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(1))
                .And(x => ThenItIsPolly(0))
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            var qosOptions = new QoSOptionsBuilder()
                .WithTimeoutValue(1)
                .WithDurationOfBreak(1)
                .WithExceptionsAllowedBeforeBreaking(1)
                .Build();

            var reRoute = new DownstreamReRouteBuilder()
                .WithQosOptions(qosOptions)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithLoadBalancerKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new ErrorResponse<IQoSProvider>(It.IsAny<Error>())))
                .And(x => GivenTheServiceProviderReturnsNothing())
                .When(x => WhenIGet())
                .Then(x => ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void ThenHandlerAtPositionIs<T>(int pos)
            where T : DelegatingHandler
        {
            var delegates = _result.Data;
            var del = delegates[pos].Invoke();
            del.ShouldBeOfType<T>();
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
            _services.AddTransient<GlobalDelegatingHandler>(s => {
                var service = s.GetService<TOne>();
                return new GlobalDelegatingHandler(service);
            });
            _services.AddTransient<TTwo>();
            _services.AddTransient<GlobalDelegatingHandler>(s => {
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
            _serviceProvider = _services.BuildServiceProvider();
        }

        private void ThenAnErrorIsReturned()
        {
            _result.IsError.ShouldBeTrue();
        }

        private void ThenTheDelegatesAreAddedCorrectly()
        {
            var delegates = _result.Data;

            var del = delegates[0].Invoke();
            var handler = (FakeDelegatingHandler) del;
            handler.Order.ShouldBe(1);

            del = delegates[1].Invoke();
            var handlerTwo = (FakeDelegatingHandlerTwo) del;
            handlerTwo.Order.ShouldBe(2);
        }

        private void GivenTheQosProviderHouseReturns(Response<IQoSProvider> qosProvider)
        {
            _qosProviderHouse
                .Setup(x => x.Get(It.IsAny<DownstreamReRoute>()))
                .Returns(qosProvider);
        }

        private void ThenItIsPolly(int i)
        {
            var delegates = _result.Data;
            var del = delegates[i].Invoke();
            del.ShouldBeOfType<PollyCircuitBreakingDelegatingHandler>();
        }

        private void ThenThereIsDelegatesInProvider(int count)
        {
            _result.ShouldNotBeNull();
            _result.Data.Count.ShouldBe(count);
        }

        private void GivenTheFollowingRequest(DownstreamReRoute request)
        {
            _request = request;
        }

        private void WhenIGet()
        {
            _serviceProvider = _services.BuildServiceProvider();
            _factory = new DelegatingHandlerHandlerFactory(_loggerFactory.Object, _tracingFactory.Object, _qosProviderHouse.Object, _serviceProvider);
            _result = _factory.Get(_request);
        }

        private void ThenNoDelegatesAreInTheProvider()
        {
            _result.ShouldNotBeNull();
            _result.Data.Count.ShouldBe(0);
        }
    }

    internal class FakeTracingHandler : DelegatingHandler, ITracingHandler
    {
    }
}
