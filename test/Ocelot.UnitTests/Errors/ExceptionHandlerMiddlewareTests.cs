using Microsoft.AspNetCore.Http;
using Ocelot.Configuration;
using Ocelot.Errors;
using Ocelot.Errors.Middleware;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;

namespace Ocelot.UnitTests.Errors
{
    public class ExceptionHandlerMiddlewareTests : UnitTest
    {
        private bool _shouldThrowAnException;
        private readonly Mock<IRequestScopedDataRepository> _repo;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly ExceptionHandlerMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpContext _httpContext;

        public ExceptionHandlerMiddlewareTests()
        {
            _httpContext = new DefaultHttpContext();
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

                _httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
            };

            _middleware = new ExceptionHandlerMiddleware(_next, _loggerFactory.Object, _repo.Object);
        }

        [Fact]
        public void NoDownstreamException()
        {
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null, null);

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
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null, null);

            this.Given(_ => GivenAnExceptionWillBeThrownDownstream())
                .And(_ => GivenTheConfigurationIs(config))
                .When(_ => WhenICallTheMiddleware())
                .Then(_ => ThenTheResponseIsError())
                .BDDfy();
        }

        [Fact]
        public void ShouldSetRequestId()
        {
            var config = new InternalConfiguration(null, null, null, "requestidkey", null, null, null, null, null, null);

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
            var config = new InternalConfiguration(null, null, null, null, null, null, null, null, null, null);

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

        private async Task WhenICallTheMiddlewareWithTheRequestIdKey(string key, string value)
        {
            _httpContext.Request.Headers.Append(key, value);
            await _middleware.Invoke(_httpContext);
        }

        private async Task WhenICallTheMiddleware()
        {
            await _middleware.Invoke(_httpContext);
        }

        private void GivenTheConfigThrows()
        {
            // this will break when we handle not having the configuratio in the items dictionary
            _httpContext.Items = new Dictionary<object, object>();
        }

        private void ThenAnExceptionIsThrown()
        {
            _httpContext.Response.StatusCode.ShouldBe(500);
        }

        private void TheRequestIdIsSet(string key, string value)
        {
            _repo.Verify(x => x.Add(key, value), Times.Once);
        }

        private void GivenTheConfigurationIs(IInternalConfiguration config)
        {
            _httpContext.Items.Add("IInternalConfiguration", config);
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
            _httpContext.Response.StatusCode.ShouldBe(200);
        }

        private void ThenTheResponseIsError()
        {
            _httpContext.Response.StatusCode.ShouldBe(500);
        }

        private void TheAspDotnetRequestIdIsSet()
        {
            _repo.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private class FakeError : Error
        {
            internal FakeError()
                : base("meh", OcelotErrorCode.CannotAddDataError, 404)
            {
            }
        }
    }
}
