using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ocelot.Library.Infrastructure.Authentication;
using Ocelot.Library.Infrastructure.DownstreamRouteFinder;
using Ocelot.Library.Infrastructure.Middleware;
using Ocelot.Library.Infrastructure.Repository;
using Ocelot.Library.Infrastructure.Responses;
using Ocelot.Library.Infrastructure.UrlMatcher;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.Middleware
{
    using Library.Infrastructure.Configuration;

    public class AuthenticationMiddlewareTests : IDisposable
    {
        private readonly Mock<IScopedRequestDataRepository> _scopedRepository;
        private readonly Mock<IAuthenticationHandlerFactory> _authFactory;
        private readonly string _url;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private HttpResponseMessage _result;
        private OkResponse<DownstreamRoute> _downstreamRoute;

        public AuthenticationMiddlewareTests()
        {
            _url = "http://localhost:51879";
            _scopedRepository = new Mock<IScopedRequestDataRepository>();
            _authFactory = new Mock<IAuthenticationHandlerFactory>();
            var builder = new WebHostBuilder()
              .ConfigureServices(x =>
              {
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
              });

            _server = new TestServer(builder);
            _client = _server.CreateClient();
        }

        [Fact]
        public void happy_path()
        {
            this.Given(x => x.GivenTheDownStreamRouteIs(new DownstreamRoute(new List<TemplateVariableNameAndValue>(), new ReRoute("","","","",false, ""))))
                .When(x => x.WhenICallTheMiddleware())
                .Then(x => x.ThenNoExceptionsAreThrown())
                .BDDfy();
        }

        private void ThenNoExceptionsAreThrown()
        {
            //todo not suck
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
