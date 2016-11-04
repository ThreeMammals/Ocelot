using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Authorisation;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Authorization
{
    using Authorisation.Middleware;

    public class AuthorisationMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IAuthoriser> _authService;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamRoute> _downstreamRoute;
        private Mock<IOcelotLoggerFactory> _mockLoggerFactory;

        public AuthorisationMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _authService = new Mock<IAuthoriser>();
            SetUpLogger();

            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton(_mockLoggerFactory.Object);
                  x.AddSingleton(_authService.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseAuthorisationMiddleware();
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), new ReRouteBuilder().WithIsAuthorised(true).Build())))
                .And(x => x.GivenTheAuthServiceReturns(new OkResponse<bool>(true)))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheAuthServiceIsCalledCorrectly())
                .BDDfy();
        }

        private void SetUpLogger()
        {
            _mockLoggerFactory = new Mock<IOcelotLoggerFactory>();

            var logger = new Mock<IOcelotLogger>();

            _mockLoggerFactory
                .Setup(x => x.CreateLogger<AuthorisationMiddleware>())
                .Returns(logger.Object);
        }

        private void GivenTheAuthServiceReturns(Response<bool> expected)
        {
            _authService
                .Setup(x => x.Authorise(It.IsAny<ClaimsPrincipal>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(expected);
        }

        private void ThenTheAuthServiceIsCalledCorrectly()
        {
            _authService
                .Verify(x => x.Authorise(It.IsAny<ClaimsPrincipal>(),
                It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        private void GivenTheDownStreamRouteIs(DownstreamRoute downstreamRoute)
        {
            _downstreamRoute = new OkResponse<DownstreamRoute>(downstreamRoute);
            _scopedRepository
                .Setup(x => x.Get<DownstreamRoute>(It.IsAny<string>()))
                .Returns(_downstreamRoute);
        }

        private void WhenICallTheMiddleware()
        {
            _result = _client.GetAsync(_url).Result;
        }


        public void Dispose()
        {
            _client.Dispose();
            _server.Dispose();
        }
    }
}
