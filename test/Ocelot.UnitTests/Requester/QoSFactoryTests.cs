using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;

namespace Ocelot.UnitTests.Requester
{
    public class QoSFactoryTests
    {
        private QoSFactory _factory;
        private ServiceCollection _services;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IHttpContextAccessor> _contextAccessor;

        public QoSFactoryTests()
        {
            _services = new ServiceCollection();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _contextAccessor = new Mock<IHttpContextAccessor>();
            var provider = _services.BuildServiceProvider();
            _factory = new QoSFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
        }

        [Fact]
        public void should_return_error()
        {
            var downstreamRoute = new DownstreamRouteBuilder().Build();
            var handler = _factory.Get(downstreamRoute);
            handler.IsError.ShouldBeTrue();
            handler.Errors[0].ShouldBeOfType<UnableToFindQoSProviderError>();
        }

        [Fact]
        public void should_return_handler()
        {
            _services = new ServiceCollection();
            DelegatingHandler QosDelegatingHandlerDelegate(DownstreamRoute a, IHttpContextAccessor b, IOcelotLoggerFactory c) => new FakeDelegatingHandler();
            _services.AddSingleton<QosDelegatingHandlerDelegate>(QosDelegatingHandlerDelegate);
            var provider = _services.BuildServiceProvider();
            _factory = new QoSFactory(provider, _contextAccessor.Object, _loggerFactory.Object);
            var downstreamRoute = new DownstreamRouteBuilder().Build();
            var handler = _factory.Get(downstreamRoute);
            handler.IsError.ShouldBeFalse();
            handler.Data.ShouldBeOfType<FakeDelegatingHandler>();
        }

        private class FakeDelegatingHandler : DelegatingHandler
        {
        }
    }
}
