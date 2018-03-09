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
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private DownstreamReRoute _request;
        private Response<List<Func<DelegatingHandler>>> _provider;
        private readonly Mock<IQosProviderHouse> _qosProviderHouse;
        private readonly Mock<ITracingHandlerFactory> _tracingFactory;
        private IServiceProvider _serviceProvider;

        public DelegatingHandlerHandlerProviderFactoryTests()
        {
            _tracingFactory = new Mock<ITracingHandlerFactory>();
            _qosProviderHouse = new Mock<IQosProviderHouse>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
        }

        [Fact]
        public void should_all_from_all_routes_provider_and_qos()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new OkResponse<IQoSProvider>(It.IsAny<PollyQoSProvider>())))
                .And(x => GivenTheServiceProviderReturns<FakeDelegatingHandler, FakeDelegatingHandlerTwo>())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(3))
                .And(x => ThenTheDelegatesAreAddedCorrectly())
                .And(x => ThenItIsPolly(2))
                .BDDfy(); 
        }

        [Fact]
        public void should_return_provider_with_no_delegates()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(false)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheServiceProviderReturnsNothing())
                .When(x => WhenIGet())
                .Then(x => ThenNoDelegatesAreInTheProvider())
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_qos_delegate()
        {
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

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
            var reRoute = new DownstreamReRouteBuilder().WithIsQos(true)
                .WithHttpHandlerOptions(new HttpHandlerOptions(true, true, false)).WithReRouteKey("").Build();

            this.Given(x => GivenTheFollowingRequest(reRoute))
                .And(x => GivenTheQosProviderHouseReturns(new ErrorResponse<IQoSProvider>(It.IsAny<Error>())))
                .And(x => GivenTheServiceProviderReturnsNothing())
                .When(x => WhenIGet())
                .Then(x => ThenAnErrorIsReturned())
                .BDDfy();
        }

        private void GivenTheServiceProviderReturns<TOne, TTwo>() 
            where TOne : DelegatingHandler
            where TTwo : DelegatingHandler
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<DelegatingHandler, TOne>();
            services.AddSingleton<DelegatingHandler, TTwo>();
            _serviceProvider = services.BuildServiceProvider();
        }

         private void GivenTheServiceProviderReturnsNothing()
        {
            IServiceCollection services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ThenAnErrorIsReturned()
        {
            _provider.IsError.ShouldBeTrue();
        }

        private void ThenTheDelegatesAreAddedCorrectly()
        {
            var delegates = _provider.Data;

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
            var delegates = _provider.Data;
            var del = delegates[i].Invoke();
            del.ShouldBeOfType<PollyCircuitBreakingDelegatingHandler>();
        }

        private void ThenThereIsDelegatesInProvider(int count)
        {
            _provider.ShouldNotBeNull();
            _provider.Data.Count.ShouldBe(count);
        }

        private void GivenTheFollowingRequest(DownstreamReRoute request)
        {
            _request = request;
        }

        private void WhenIGet()
        {
            _factory = new DelegatingHandlerHandlerFactory(_loggerFactory.Object, _tracingFactory.Object, _qosProviderHouse.Object, _serviceProvider);
            _provider = _factory.Get(_request);
        }

        private void ThenNoDelegatesAreInTheProvider()
        {
            _provider.ShouldNotBeNull();
            _provider.Data.Count.ShouldBe(0);
        }
    }
}
