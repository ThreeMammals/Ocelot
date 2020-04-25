namespace Ocelot.UnitTests.Errors
{
    using Microsoft.AspNetCore.Http;
    using Moq;
    using Ocelot.Configuration;
    using Ocelot.Configuration.Repository;
    using Ocelot.Errors;
    using Ocelot.Errors.Middleware;
    using Ocelot.Infrastructure.RequestData;
    using Ocelot.Logging;
    using Ocelot.Middleware;
    using Shouldly;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using TestStack.BDDfy;
    using Xunit;

    public class ExceptionHandlerMiddlewareTests
    {
        private bool _shouldThrowAnException;
        private readonly Mock<IRequestScopedDataRepository> _repo;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly ExceptionHandlerMiddleware _middleware;
        private RequestDelegate _next;
        private Mock<HttpContext> _httpContext;

        public ExceptionHandlerMiddlewareTests()
        {
            _httpContext = new Mock<HttpContext>();
            _repo = new Mock<IRequestScopedDataRepository>();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<ExceptionHandlerMiddleware>()).Returns(_logger.Object);
            _next = async context =>
            {
                await Task.CompletedTask;

                if (_shouldThrowAnException)
                {
                    throw new Exception("BOOM");
                }

                _httpContext.Object.Response.StatusCode = (int)HttpStatusCode.OK;
            };

            _middleware = new ExceptionHandlerMiddleware(_next, _loggerFactory.Object, _repo.Object);
        }

        [Fact]
        public void NoDownstreamException()
        {
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheAspDotnetRequestIdIsSet())
                .BDDfy();
        }

        [Fact]
        public void DownstreamException()
        {
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null);

            this.Given(_ => GivenAnExceptionWillBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        [Fact]
        public void ShouldSetRequestId()
        {
            var config = new InternalConfiguration(null, null, null, "requestidkey", null, null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheRequestIdIsSet("RequestId", "1234"))
                .BDDfy();
        }

        [Fact]
        public void ShouldSetAspDotNetRequestId()
        {
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null);

            this.Given(_ => GivenAnExceptionWillNotBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddlewareWithTheRequestIdKey("requestidkey", "1234"))
                .Then(_ => ThenTheResponseIsOk())
                .And(_ => TheAspDotnetRequestIdIsSet())
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
            _httpContext.Setup(x => x.Request.Headers).Returns(new HeaderDictionary() { { key, value } });
            _middleware.Invoke(_httpContext.Object).GetAwaiter().GetResult();
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_httpContext.Object).GetAwaiter().GetResult();
        }

        private void GivenTheConfigThrows()
        {
            var ex = new Exception("outer", new Exception("inner"));
            _httpContext.Setup(x => x.Items).Throws(ex);
            //_downstreamContext
            //   .Setup(x => x.Configuration).Throws(ex);
        }

        private void ThenAnExceptionIsThrown()
        {
            _httpContext.Object.Response.StatusCode.ShouldBe(500);
        }

        private void TheRequestIdIsSet(string key, string value)
        {
            _repo.Verify(x => x.Add(key, value), Times.Once);
        }

        private void GivenTheConfigurationIs(IInternalConfiguration config)
        {
            _httpContext.Setup(x => x.Items).Returns(new Dictionary<object, object>() { { "IInternalConfiguration", config } });
            //_downstreamContext
            //    .Setup(x => x.Configuration).Returns(config);
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
            _httpContext.Object.Response.StatusCode.ShouldBe(200);
        }

        private void ThenTheResponseIsError()
        {
            _httpContext.Object.Response.StatusCode.ShouldBe(500);
        }

        private void TheAspDotnetRequestIdIsSet()
        {
            _repo.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private class FakeError : Error
        {
            internal FakeError()
                : base("meh", OcelotErrorCode.CannotAddDataError)
            {
            }
        }
    }
}
