using Moq;
using Ocelot.Logging;
using Ocelot.Requester;
using Ocelot.Requester.QoS;
using Shouldly;
using Xunit;

namespace Ocelot.UnitTests.Requester
{
    public class HttpClientBuilderTests
    {
        private HttpClientBuilder _builder;
        private Mock<IDelegatingHandlerHandlerProvider> _provider;
        private Mock<IQoSProvider> _qosProvider;
        private Mock<IOcelotLogger> _logger;

        public HttpClientBuilderTests()
        {
            _logger = new Mock<IOcelotLogger>();
            _qosProvider = new Mock<IQoSProvider>();
            _provider = new Mock<IDelegatingHandlerHandlerProvider>();
            _builder = new HttpClientBuilder(_provider.Object);
        }

        [Fact]
        public void should_build_http_client()
        {
            var httpClient = _builder.Create(false, false);
            httpClient.ShouldNotBeNull();
        }

        [Fact]
        public void should_add_qos()
        {
            var builder = _builder.WithQos(_qosProvider.Object, _logger.Object);
            builder.ShouldNotBeNull();
        }
    }

    public class DelegatingHandlerHandlerProviderTests
    {
        private DelegatingHandlerHandlerProvider _provider;

        public DelegatingHandlerHandlerProviderTests()
        {
            _provider = new DelegatingHandlerHandlerProvider();
        }
        [Fact]
        public void should_add_delegating_handler()
        {
            _provider.Add(1, )
        }

        [Fact]
        public void should_get_delegating_handlers()
        {

        }
    }
}