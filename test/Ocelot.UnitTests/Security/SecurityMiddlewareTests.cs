using Microsoft.AspNetCore.Http;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security;
using Ocelot.Security.Middleware;

namespace Ocelot.UnitTests.Security
{
    public class SecurityMiddlewareTests : UnitTest
    {
        private readonly List<Mock<ISecurityPolicy>> _securityPolicyList;
        private readonly Mock<IOcelotLoggerFactory> _loggerFactory;
        private readonly Mock<IOcelotLogger> _logger;
        private readonly SecurityMiddleware _middleware;
        private readonly RequestDelegate _next;
        private readonly HttpContext _httpContext;

        public SecurityMiddlewareTests()
        {
            _httpContext = new DefaultHttpContext();
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<SecurityMiddleware>()).Returns(_logger.Object);
            _securityPolicyList = new List<Mock<ISecurityPolicy>>
            {
                new(),
                new(),
            };
            _next = context => Task.CompletedTask;
            _middleware = new SecurityMiddleware(_next, _loggerFactory.Object, _securityPolicyList.Select(f => f.Object).ToList());
            _httpContext.Items.UpsertDownstreamRequest(new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com")));
        }

        [Fact]
        public void Should_legal_request()
        {
            this.Given(x => x.GivenPassingSecurityVerification())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheRequestIsPassingSecurity())
                .BDDfy();
        }

        [Fact]
        public void Should_verification_failed_request()
        {
            this.Given(x => x.GivenNotPassingSecurityVerification())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheRequestIsNotPassingSecurity())
                .BDDfy();
        }

        private void GivenPassingSecurityVerification()
        {
            foreach (var item in _securityPolicyList)
            {
                Response response = new OkResponse();
                item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(Task.FromResult(response));
            }
        }

        private void GivenNotPassingSecurityVerification()
        {
            for (var i = 0; i < _securityPolicyList.Count; i++)
            {
                var item = _securityPolicyList[i];
                if (i == 0)
                {
                    Error error = new UnauthenticatedError("Not passing security verification");
                    Response response = new ErrorResponse(error);
                    item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(Task.FromResult(response));
                }
                else
                {
                    Response response = new OkResponse();
                    item.Setup(x => x.Security(_httpContext.Items.DownstreamRoute(), _httpContext)).Returns(Task.FromResult(response));
                }
            }
        }

        private Task WhenICallTheMiddleware() => _middleware.Invoke(_httpContext);

        private void ThenTheRequestIsPassingSecurity() => _httpContext.Items.Errors().Count.ShouldBe(0);

        private void ThenTheRequestIsNotPassingSecurity() => _httpContext.Items.Errors().Count.ShouldBeGreaterThan(0);
    }
}
