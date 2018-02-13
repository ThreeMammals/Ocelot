using System;
using System.Collections.Generic;
using System.Net.Http;
using Moq;
using Ocelot.Logging;
using Ocelot.Requester;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class DelegatingHandlerHandlerProviderFactoryTests
    {
        private readonly DelegatingHandlerHandlerProviderFactory _factory;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Ocelot.Request.Request _request;
        private IDelegatingHandlerHandlerProvider _provider;
        private readonly Mock<IDelegatingHandlerHandlerProvider> _allRoutesProvider;

        public DelegatingHandlerHandlerProviderFactoryTests()
        {
            _allRoutesProvider = new Mock<IDelegatingHandlerHandlerProvider>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _factory = new DelegatingHandlerHandlerProviderFactory(_loggerFactory.Object, _allRoutesProvider.Object, null);
        }

        [Fact]
        public void should_all_from_all_routes_provider_and_qos()
        {
            var handlers = new List<Func<DelegatingHandler>>
            {
                () => new FakeDelegatingHandler(0),
                () => new FakeDelegatingHandler(1)
            };

            var request = new Ocelot.Request.Request(new HttpRequestMessage(), true, null, true, true, "", false);

            this.Given(x => GivenTheFollowingRequest(request))
                .And(x => GivenTheAllRoutesProviderReturns(handlers))
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(3))
                .And(x => ThenTheDelegatesAreAddedCorrectly())
                .And(x => ThenItIsPolly(2))
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_no_delegates()
        {
            var request = new Ocelot.Request.Request(new HttpRequestMessage(), false, null, true, true, "", false);

            this.Given(x => GivenTheFollowingRequest(request))
                .And(x => GivenTheAllRoutesProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenNoDelegatesAreInTheProvider())
                .BDDfy();
        }

        [Fact]
        public void should_return_provider_with_qos_delegate()
        {
            var request = new Ocelot.Request.Request(new HttpRequestMessage(), true, null, true, true, "", false);

            this.Given(x => GivenTheFollowingRequest(request))
                .And(x => GivenTheAllRoutesProviderReturns())
                .When(x => WhenIGet())
                .Then(x => ThenThereIsDelegatesInProvider(1))
                .And(x => ThenItIsPolly(0))
                .BDDfy();
        }

        private void ThenTheDelegatesAreAddedCorrectly()
        {
            var delegates = _provider.Get();
            var del = delegates[0].Invoke();
            var handler = (FakeDelegatingHandler) del;
            handler.Order.ShouldBe(0);

            del = delegates[1].Invoke();
            handler = (FakeDelegatingHandler)del;
            handler.Order.ShouldBe(1);
        }

        private void GivenTheAllRoutesProviderReturns()
        {
            _allRoutesProvider.Setup(x => x.Get()).Returns(new List<Func<DelegatingHandler>>());
        }

        private void GivenTheAllRoutesProviderReturns(List<Func<DelegatingHandler>> handlers)
        {
            _allRoutesProvider.Setup(x => x.Get()).Returns(handlers);
        }

        private void ThenItIsPolly(int i)
        {
            var delegates = _provider.Get();
            var del = delegates[i].Invoke();
            del.ShouldBeOfType<PollyCircuitBreakingDelegatingHandler>();
        }

        private void ThenThereIsDelegatesInProvider(int count)
        {
            _provider.ShouldNotBeNull();
            _provider.Get().Count.ShouldBe(count);
        }

        private void GivenTheFollowingRequest(Ocelot.Request.Request request)
        {
            _request = request;
        }

        private void WhenIGet()
        {
            _provider = _factory.Get(_request);
        }

        private void ThenNoDelegatesAreInTheProvider()
        {
            _provider.ShouldNotBeNull();
            _provider.Get().Count.ShouldBe(0);
        }
    }
}
