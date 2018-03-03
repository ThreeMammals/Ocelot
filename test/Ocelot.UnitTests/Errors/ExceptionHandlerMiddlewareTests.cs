using Ocelot.Middleware;

namespace Ocelot.UnitTests.Errors
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Ocelot.Errors.Middleware;
    using Ocelot.Logging;
    using Shouldly;
    using TestStack.BDDfy;
    using Xunit;
    using Microsoft.AspNetCore.Http;
    using Ocelot.Configuration.Provider;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Errors;
    using Ocelot.DownstreamRouteFinder.Middleware;
    using Ocelot.Infrastructure.RequestData;

    public class ExceptionHandlerMiddlewareTests
    {
        bool _shouldThrowAnException = false;
        private Mock<IOcelotConfigurationProvider> _provider;
        private Mock<IRequestScopedDataRepository> _repo;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private ExceptionHandlerMiddleware _middleware;
        private DownstreamContext _downstreamContext;
        private OcelotRequestDelegate _next;

        public ExceptionHandlerMiddlewareTests()
        {
            _provider = new Mock<IOcelotConfigurationProvider>();
            _repo = new Mock<IRequestScopedDataRepository>();
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ExceptionHandlerMiddleware>()).Returns(_logger.Object);
            _next = async context => {
                await Task.CompletedTask;

                if (_shouldThrowAnException)
                {
                    throw new Exception("BOOM");
                }

                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            };
            _middleware = new ExceptionHandlerMiddleware(_next, _loggerFactory.Object, _provider.Object, _repo.Object);
        }
        
        [Fact]
        public void NoDownstreamException()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsNotSet())
                .BDDfy();
        }

        [Fact]
        public void DownstreamException()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        [Fact]
        public void ShouldSetRequestId()
        {
            var config = new OcelotConfiguration(null, null, null, "requestidkey");

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsSet("RequestId", "1234"))
                .BDDfy();
        }

        [Fact]
        public void ShouldNotSetRequestId()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsNotSet())
                .BDDfy();
        }

        [Fact]
        public void should_throw_exception_if_config_provider_returns_error()
        {
             this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigReturnsError())
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenAnExceptionIsThrown())
                .BDDfy();
        }

        [Fact]
        public void should_throw_exception_if_config_provider_throws()
        {
             this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigThrows())
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenAnExceptionIsThrown())
                .BDDfy();
        }

        private void WhenICallTheMiddlewareWithTheRequestIdKey(string key, string value)
        {
            _downstreamContext.HttpContext.Request.Headers.Add(key, value);
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void GivenTheConfigThrows()
        {
            var ex = new Exception("outer", new Exception("inner"));
             _provider
                .Setup(x => x.Get()).ThrowsAsync(ex);
        }

        private void ThenAnExceptionIsThrown()
        {
            _downstreamContext.HttpContext.Response.StatusCode.ShouldBe(500);
        }

        private void GivenTheConfigReturnsError()
        {
            var config = new OcelotConfiguration(null, null, null, null);

            var response = new Ocelot.Responses.ErrorResponse<IOcelotConfiguration>(new FakeError());
            _provider
                .Setup(x => x.Get()).ReturnsAsync(response);
        }

        public class FakeError : Error
        {
            public FakeError() 
                : base("meh", OcelotErrorCode.CannotAddDataError)
            {
            }
        }

        private void TheRequestIdIsSet(string key, string value)
        {
            _repo.Verify(x => x.Add<string>(key, value), Times.Once);
        }

        private void GivenTheConfigurationIs(IOcelotConfiguration config)
        {
            var response = new Ocelot.Responses.OkResponse<IOcelotConfiguration>(config);
            _provider
                .Setup(x => x.Get()).ReturnsAsync(response);
        }

        private void GivenAnExceptionWillNotBeThrownDownstream()
        {
            _shouldThrowAnException = false;
        }

        private void GivenAnExceptionWillBeThrownDownstream()
        {
            _shouldThrowAnException = true;
        }

        private void ThenTheResponseIsOk()
        {
            _downstreamContext.HttpContext.Response.StatusCode.ShouldBe(200);
        }

        private void ThenTheResponseIsError()
        {
            _downstreamContext.HttpContext.Response.StatusCode.ShouldBe(500);
        }

        private void TheRequestIdIsNotSet()
        {
            _repo.Verify(x => x.Add<string>(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
