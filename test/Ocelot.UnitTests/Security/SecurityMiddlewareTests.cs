using Microsoft.AspNetCore.Http;
using Moq;
using Ocelot.Errors;
using Ocelot.Logging;
using Ocelot.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Responses;
using Ocelot.Security;
using Ocelot.Security.Middleware;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Security
{
    public class SecurityMiddlewareTests
    {
        private List<Mock<ISecurityPolicy>> _securityPolicyList;
        private Mock<IOcelotLoggerFactory> _loggerFactory;
        private Mock<IOcelotLogger> _logger;
        private readonly SecurityMiddleware _middleware;
        private readonly DownstreamContext _downstreamContext;
        private readonly OcelotRequestDelegate _next;

        public SecurityMiddlewareTests()
        {
            _loggerFactory = new Mock<IOcelotLoggerFactory>();
            _logger = new Mock<IOcelotLogger>();
            _loggerFactory.Setup(x => x.CreateLogger<SecurityMiddleware>()).Returns(_logger.Object);
            _securityPolicyList = new List<Mock<ISecurityPolicy>>();
            _securityPolicyList.Add(new Mock<ISecurityPolicy>());
            _securityPolicyList.Add(new Mock<ISecurityPolicy>());
            _next = context =>
            {
                return Task.CompletedTask;
            };
            _middleware = new SecurityMiddleware(_loggerFactory.Object, _securityPolicyList.Select(f => f.Object).ToList(), _next);
            _downstreamContext = new DownstreamContext(new DefaultHttpContext());
            _downstreamContext.DownstreamRequest = new DownstreamRequest(new HttpRequestMessage(HttpMethod.Get, "http://test.com"));
        }

        [Fact]
        public void should_legal_request()
        {
            this.Given(x => x.GivenPassingSecurityVerification())
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheRequestIsPassingSecurity())
                .BDDfy();
        }

        [Fact]
        public void should_verification_failed_request()
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
                item.Setup(x => x.Security(_downstreamContext)).Returns(Task.FromResult(response));
            }
        }

        private void GivenNotPassingSecurityVerification()
        {
            for (int i = 0; i < _securityPolicyList.Count; i++)
            {
                Mock<ISecurityPolicy> item = _securityPolicyList[i];
                if (i == 0)
                {
                    Error error = new UnauthenticatedError($"Not passing security verification");
                    Response response = new ErrorResponse(error);
                    item.Setup(x => x.Security(_downstreamContext)).Returns(Task.FromResult(response));
                }
                else
                {
                    Response response = new OkResponse();
                    item.Setup(x => x.Security(_downstreamContext)).Returns(Task.FromResult(response));
                }
            }
        }

        private void WhenICallTheMiddleware()
        {
            _middleware.Invoke(_downstreamContext).GetAwaiter().GetResult();
        }

        private void ThenTheRequestIsPassingSecurity()
        {
            Assert.False(_downstreamContext.IsError);
        }

        private void ThenTheRequestIsNotPassingSecurity()
        {
            Assert.True(_downstreamContext.IsError);
        }
    }
}
