using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Authentication.Handler;
using Ocelot.Authentication.Handler.Factory;
using Ocelot.Authentication.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Infrastructure.RequestData;
using Ocelot.Logging;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Authentication
{
    public class AuthenticationMiddlewareTests : IDisposable
    {
        private readonly Mock<IRequestScopedDataRepository> _scopedRepository;
        private readonly Mock<IAuthenticationHandlerFactory> _authFactory;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public AuthenticationMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IRequestScopedDataRepository>();
            _authFactory = new Mock<IAuthenticationHandlerFactory>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
                  x.AddSingleton<IOcelotLoggerFactory, AspDotNetLoggerFactory>();
                  x.AddLogging();
                  x.AddSingleton(_authFactory.Object);
                  x.AddSingleton(_scopedRepository.Object);
              })
              .UseUrls(_url)
              .UseKestrel()
              .UseContentRoot(Directory.GetCurrentDirectory())
              .UseIISIntegration()
              .UseUrls(_url)
              .Configure(app =>
              {
                  app.UseAuthenticationMiddleware();

                  app.Run(async x =>
                  {
                      await x.Response.WriteAsync("The user is authenticated");
                  });
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void should_call_next_middleware_if_route_is_not_authenticated()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<UrlPathPlaceholderNameAndValue>(), new ReRouteBuilder()
                                                                                                                            .WithUpstreamHttpMethod("Get")
                                                                                                                            .Build())))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenTheUserIsAuthenticated())
                .BDDfy();
        }

        private void ThenTheUserIsAuthenticated()
        {
            var content = _result.Content.ReadAsStringAsync().Result;
            content.ShouldBe("The user is authenticated");
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
